using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MonsterPool : SingletonBehaviour<MonsterPool>
{
    [Serializable]
    public class PreloadEntry
    {
        public string EnemyId;
        public int Count;
    }

    [Header("Addressables")]
    [SerializeField] private string _enemyAddressablePrefix = "Enemy/";

    [Header("Preload Settings")]
    [SerializeField] private PreloadEntry[] _preloadList;

    [Header("Pool Parent")]
    [SerializeField] private Transform _poolParent;

    [Header("Game Targets")]
    [SerializeField] private Transform _spiritTree;
    [SerializeField] private Transform _player;

    public Transform SpiritTree => _spiritTree;
    public Transform Player => _player;

    private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    private Dictionary<GameObject, string> _instanceToType = new Dictionary<GameObject, string>();
    private List<AsyncOperationHandle<GameObject>> _loadedHandles = new List<AsyncOperationHandle<GameObject>>();

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();

        if (_poolParent == null)
        {
            var poolObj = new GameObject("PooledMonsters");
            poolObj.transform.SetParent(transform);
            _poolParent = poolObj.transform;
        }
    }

    private void Start()
    {
        LoadAndPreloadAll();
    }

    private void LoadAndPreloadAll()
    {
        if (_preloadList == null || _preloadList.Length == 0)
        {
            _isInitialized = true;
            return;
        }

        int totalToLoad = _preloadList.Length;
        int loadedCount = 0;

        for (int i = 0; i < _preloadList.Length; i++)
        {
            var entry = _preloadList[i];
            LoadPrefab(entry.EnemyId, () =>
            {
                loadedCount++;
                if (loadedCount >= totalToLoad)
                {
                    PreloadAll();
                }
            });
        }
    }

    private void LoadPrefab(string enemyId, Action onComplete)
    {
        if (_prefabs.ContainsKey(enemyId))
        {
            onComplete?.Invoke();
            return;
        }

        string address = $"{_enemyAddressablePrefix}{enemyId}";
        var handle = Addressables.LoadAssetAsync<GameObject>(address);

        handle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _prefabs[enemyId] = op.Result;
                _pools[enemyId] = new Queue<GameObject>();
                _loadedHandles.Add(handle);
            }
            else
            {
                Debug.LogError($"[MonsterPool] Failed to load: {address}");
            }
            onComplete?.Invoke();
        };
    }

    private void PreloadAll()
    {
        for (int i = 0; i < _preloadList.Length; i++)
        {
            var entry = _preloadList[i];
            Preload(entry.EnemyId, entry.Count);
        }

        _isInitialized = true;
    }

    public void RegisterPrefab(string enemyType, GameObject prefab)
    {
        if (!_prefabs.ContainsKey(enemyType))
        {
            _prefabs[enemyType] = prefab;
        }

        if (!_pools.ContainsKey(enemyType))
        {
            _pools[enemyType] = new Queue<GameObject>();
        }
    }

    public void Preload(string enemyType, Action onComplete = null)
    {
        LoadPrefab(enemyType, onComplete);
    }

    public void Preload(string enemyType, int count)
    {
        if (!_prefabs.TryGetValue(enemyType, out var prefab))
        {
            return;
        }

        if (!_pools.ContainsKey(enemyType))
        {
            _pools[enemyType] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            var instance = CreateInstance(enemyType, prefab);
            instance.SetActive(false);
            _pools[enemyType].Enqueue(instance);
        }

    }

    public GameObject Spawn(string enemyType, Vector3 position, Quaternion rotation, float statMultiplier = 1f)
    {
        if (!_pools.TryGetValue(enemyType, out var pool))
        {
            if (!_prefabs.TryGetValue(enemyType, out var prefab))
            {
                Debug.LogError($"[MonsterPool] Unknown enemy type: {enemyType}");
                return null;
            }
            
            pool = new Queue<GameObject>();
            _pools[enemyType] = pool;
        }

        GameObject instance;

        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
            instance.transform.SetParent(null);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }
        else
        {
            if (!_prefabs.TryGetValue(enemyType, out var prefab))
            {
                return null;
            }
            instance = CreateInstance(enemyType, prefab);
            instance.transform.SetParent(null);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }

        instance.SetActive(true);
        InjectTargets(instance);
        
        
        if (statMultiplier > 1f)
        {
            ApplyStatMultiplier(instance, statMultiplier);
        }
        
        return instance;
    }

    private void ApplyStatMultiplier(GameObject instance, float multiplier)
    {
        if (instance.TryGetComponent<KamikazeController>(out var kamikaze))
        {
            kamikaze.StatMultiplier = multiplier;
            kamikaze.ReapplyHealth();
        }
        else if (instance.TryGetComponent<EnemyController>(out var melee))
        {
            melee.StatMultiplier = multiplier;
            melee.ReapplyHealth();
        }
        else if (instance.TryGetComponent<EnemyRangedController>(out var ranged))
        {
            ranged.StatMultiplier = multiplier;
            ranged.ReapplyHealth();
        }
        else if (instance.TryGetComponent<TankController>(out var tank))
        {
            tank.StatMultiplier = multiplier;
            tank.ReapplyHealth();
        }
        else if (instance.TryGetComponent<SpawnerController>(out var spawner))
        {
            spawner.StatMultiplier = multiplier;
            spawner.ReapplyHealth();
        }
    }

    private void InjectTargets(GameObject instance)
    {
        if (instance.TryGetComponent<EnemyMovement>(out var melee))
        {
            melee.SetTargets(_spiritTree, _player);
        }
        else if (instance.TryGetComponent<EnemyRangedMovement>(out var ranged))
        {
            ranged.SetTargets(_spiritTree, _player);
        }
        else if (instance.TryGetComponent<TankMovement>(out var tank))
        {
            tank.SetTargets(_spiritTree, _player);
        }
        else if (instance.TryGetComponent<KamikazeMovement>(out var kamikaze))
        {
            kamikaze.SetTargets(_spiritTree);
        }
        else if (instance.TryGetComponent<SpawnerController>(out var spawner))
        {
            spawner.SetTargets(_spiritTree);
        }
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!_instanceToType.TryGetValue(instance, out var enemyType))
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(_poolParent);

        if (_pools.TryGetValue(enemyType, out var pool))
        {
            pool.Enqueue(instance);
        }
    }

    private GameObject CreateInstance(string enemyType, GameObject prefab)
    {
        var instance = Instantiate(prefab, _poolParent);
        _instanceToType[instance] = enemyType;
        return instance;
    }

    public void ClearPool(string enemyType)
    {
        if (!_pools.TryGetValue(enemyType, out var pool))
        {
            return;
        }

        while (pool.Count > 0)
        {
            var instance = pool.Dequeue();
            if (instance != null)
            {
                _instanceToType.Remove(instance);
                Destroy(instance);
            }
        }
    }

    public void ClearAllPools()
    {
        foreach (var kvp in _pools)
        {
            while (kvp.Value.Count > 0)
            {
                var instance = kvp.Value.Dequeue();
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }

        _pools.Clear();
        _instanceToType.Clear();
    }

    protected override void Dispose()
    {
        ClearAllPools();
        _prefabs.Clear();

        for (int i = 0; i < _loadedHandles.Count; i++)
        {
            if (_loadedHandles[i].IsValid())
            {
                Addressables.Release(_loadedHandles[i]);
            }
        }
        _loadedHandles.Clear();

        base.Dispose();
    }
}
