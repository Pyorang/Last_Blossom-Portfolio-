using System.Collections.Generic;
using UnityEngine;

public class SpawnEffectPool : SingletonBehaviour<SpawnEffectPool>
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private float _effectDuration = 2f;
    
    private Queue<GameObject> _pool = new Queue<GameObject>();
    private Transform _poolParent;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
        
        
        _poolParent = new GameObject("SpawnEffectPool").transform;
        _poolParent.SetParent(transform);
        
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        if (_effectPrefab == null)
        {
            Debug.LogError("[SpawnEffectPool] Effect prefab is not assigned!");
            return;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            var effect = CreateNewEffect();
            effect.SetActive(false);
            _pool.Enqueue(effect);
        }
    }

    private GameObject CreateNewEffect()
    {
        var effect = Instantiate(_effectPrefab, _poolParent);
        
        // 자동 반환 컴포넌트 추가
        var autoReturn = effect.GetComponent<PooledEffect>();
        if (autoReturn == null)
        {
            autoReturn = effect.AddComponent<PooledEffect>();
        }
        autoReturn.Initialize(this);
        autoReturn.SetDuration(_effectDuration);
        
        return effect;
    }

    public GameObject Spawn(Vector3 position)
    {
        
        GameObject effect;
        
        if (_pool.Count > 0)
        {
            effect = _pool.Dequeue();
        }
        else
        {
            effect = CreateNewEffect();
        }
        
        position.y = 0f;
        effect.transform.position = position;
        effect.SetActive(true);
        
        return effect;
    }

    public void Return(GameObject effect)
    {
        if (effect == null) return;
        
        effect.SetActive(false);
        _pool.Enqueue(effect);
    }
}
