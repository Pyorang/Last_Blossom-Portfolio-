using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : SingletonBehaviour<WaveManager>
{
    [Header("Portal Settings")]
    [SerializeField] private Portal[] _portals;
    [SerializeField] private Vector3 _spawnOffset = Vector3.zero;
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _effectToSpawnDelay = 1f;

    [Header("Infinite Wave Settings")]
    [SerializeField] private int _lastDesignedWave = 10;
    [SerializeField] private int _maxWave = 20;
    [SerializeField] private int _baseMonsterCount = 8;
    [SerializeField] private int _monstersPerWave = 2;
    [SerializeField] private float _hpMultiplierPerWave = 0.1f;
    [SerializeField] private float _attackMultiplierPerWave = 0.05f;
    [SerializeField] private InfiniteWaveEnemyPool[] _infiniteEnemyPools;

    [Header("Debug")]
    [SerializeField] private int _currentWaveId = 0;
    [SerializeField] private bool _isWaveActive = false;

    private Dictionary<int, Portal> _portalDict = new Dictionary<int, Portal>();

    private List<Coroutine> _activeSpawnCoroutines = new List<Coroutine>();
    private List<GameObject> _activeMonsters = new List<GameObject>();
    private int _totalMonstersInWave;
    private int _remainingMonsters;

    private List<WaveData> _tempWaveEntries = new List<WaveData>(32);
    private WaitForSeconds _spawnIntervalWait;
    private WaitForSeconds _effectToSpawnDelayWait;

    private float _currentStatMultiplier = 1f;

    private int RemainingMonsters
    {
        get => _remainingMonsters;
        set
        {
            _remainingMonsters = value;
            OnMonsterCountChanged?.Invoke(_currentWaveId, _remainingMonsters);

            if (_remainingMonsters <= 0)
            {
                OnAllMonstersDefeated();
            }
        }
    }

    public event Action<int> OnWaveStart;
    public event Action<int> OnWaveEnd;
    public event Action<int, int> OnMonsterCountChanged;
    public event Action<int> OnWaveCleared;
    public event Action<int> OnWaveFailed;
    public event Action OnAllWavesCleared;

    public bool IsWaveActive => _isWaveActive;
    public int CurrentWaveId => _currentWaveId;
    public int TotalWaveCount => DataTableManager.Instance.GetTotalWaveCount();
    public bool IsInfiniteWave => _currentWaveId > _lastDesignedWave;
    public float CurrentStatMultiplier => _currentStatMultiplier;
    public int MaxWave => _maxWave;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
        
        _spawnIntervalWait = new WaitForSeconds(_spawnInterval);
        _effectToSpawnDelayWait = new WaitForSeconds(_effectToSpawnDelay);
        InitializePortalDictionary();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void InitializePortalDictionary()
    {
        for (int i = 0; i < _portals.Length; i++)
        {
            var portal = _portals[i];
            if (portal != null)
            {
                _portalDict[portal.PortalId] = portal;
            }
        }
    }

    private void CollectWaveEntries(int waveId)
    {
        _tempWaveEntries.Clear();
        _totalMonstersInWave = 0;


        // 무한 웨이브인 경우 프로시저럴 생성
        if (waveId > _lastDesignedWave)
        {
            GenerateInfiniteWaveEntries(waveId);
            return;
        }

        // 기존 CSV 기반 로직
        var waveData = DataTableManager.Instance.GetWaveData(waveId);
        if (waveData == null) return;

        for (int i = 0; i < waveData.Count; i++)
        {
            _tempWaveEntries.Add(waveData[i]);
            _totalMonstersInWave += waveData[i].Count;
        }

        _currentStatMultiplier = 1f;
    }

    private void GenerateInfiniteWaveEntries(int waveId)
    {
        
        int infiniteLevel = waveId - _lastDesignedWave;

        // 스탯 배율 계산
        _currentStatMultiplier = 1f + (infiniteLevel * _hpMultiplierPerWave);

        // 총 몬스터 수 계산
        int totalCount = _baseMonsterCount + (infiniteLevel * _monstersPerWave);

        // 적 구성 생성
        var enemyCounts = GenerateEnemyComposition(infiniteLevel, totalCount);
        int portalCount = _portals.Length;
        int portalIndex = 0;

        foreach (var kvp in enemyCounts)
        {
            if (kvp.Value <= 0) continue;

            // 포탈 순환 배치
            int portalId = _portals[portalIndex % portalCount].PortalId;
            portalIndex++;

            var entry = new WaveData
            {
                WaveId = waveId,
                EnemyType = kvp.Key,
                Count = kvp.Value,
                PortalId = portalId,
                PortalDelay = portalIndex * 0.5f
            };

            _tempWaveEntries.Add(entry);
            _totalMonstersInWave += kvp.Value;
        }

    }

    private Dictionary<string, int> GenerateEnemyComposition(int infiniteLevel, int totalCount)
    {
        var result = new Dictionary<string, int>();
        var availableEnemies = new List<(string type, float weight)>();

        // 현재 웨이브에서 등장 가능한 적과 가중치 계산
        for (int i = 0; i < _infiniteEnemyPools.Length; i++)
        {
            var pool = _infiniteEnemyPools[i];
            int currentWave = _lastDesignedWave + infiniteLevel;

            if (currentWave < pool.MinWaveToAppear)
                continue;

            float weight = pool.BaseWeight + (infiniteLevel * pool.WeightIncreasePerWave);
            weight = Mathf.Max(0f, weight);

            if (weight > 0f)
                availableEnemies.Add((pool.EnemyType, weight));
        }

        if (availableEnemies.Count == 0)
            return result;

        // 가중치 합계 계산
        float totalWeight = 0f;
        for (int i = 0; i < availableEnemies.Count; i++)
            totalWeight += availableEnemies[i].weight;

        // 가중치 기반 랜덤 선택으로 몬스터 배분
        for (int i = 0; i < totalCount; i++)
        {
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int j = 0; j < availableEnemies.Count; j++)
            {
                cumulative += availableEnemies[j].weight;
                if (roll <= cumulative)
                {
                    string enemyType = availableEnemies[j].type;
                    if (!result.ContainsKey(enemyType))
                        result[enemyType] = 0;
                    result[enemyType]++;
                    break;
                }
            }
        }

        return result;
    }

    public void PreloadWaveEnemies(int waveId, Action onComplete = null)
    {
        HashSet<string> enemyTypes;

        if (waveId > _lastDesignedWave)
        {
            enemyTypes = GetInfiniteWaveEnemyTypes(waveId);
        }
        else
        {
            enemyTypes = DataTableManager.Instance.GetEnemyTypesInWave(waveId);
        }

        if (enemyTypes.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int loadedCount = 0;
        int totalToLoad = enemyTypes.Count;

        foreach (var enemyType in enemyTypes)
        {
            MonsterPool.Instance.Preload(enemyType, () =>
            {
                loadedCount++;
                if (loadedCount >= totalToLoad)
                {
                    onComplete?.Invoke();
                }
            });
        }
    }

    private HashSet<string> GetInfiniteWaveEnemyTypes(int waveId)
    {
        var types = new HashSet<string>();

        for (int i = 0; i < _infiniteEnemyPools.Length; i++)
        {
            var pool = _infiniteEnemyPools[i];
            if (waveId >= pool.MinWaveToAppear)
            {
                types.Add(pool.EnemyType);
            }
        }

        return types;
    }

    public void StartWave(int waveId)
    {
        if (_isWaveActive)
        {
            Debug.LogWarning($"[WaveManager] Wave {_currentWaveId} is already active!");
            return;
        }

        CollectWaveEntries(waveId);

        if (_tempWaveEntries.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] No wave data found for Wave {waveId}");
            return;
        }

        PreloadWaveEnemies(waveId, () =>
        {
            StartWaveInternal(waveId);
        });
    }

    private void StartWaveInternal(int waveId)
    {
        _currentWaveId = waveId;
        _isWaveActive = true;

        _activeMonsters.Clear();

        OnWaveStart?.Invoke(waveId);
        RemainingMonsters = _totalMonstersInWave;

        for (int i = 0; i < _tempWaveEntries.Count; i++)
        {
            var coroutine = StartCoroutine(SpawnGroup(_tempWaveEntries[i]));
            _activeSpawnCoroutines.Add(coroutine);
        }
    }

    public void StartNextWave()
    {
        int nextWaveId = _currentWaveId + 1;

        if (HasNextWave())
        {
            StartWave(nextWaveId);
        }
    }

    public void StopCurrentWave()
    {
        for (int i = 0; i < _activeSpawnCoroutines.Count; i++)
        {
            if (_activeSpawnCoroutines[i] != null)
            {
                StopCoroutine(_activeSpawnCoroutines[i]);
            }
        }
        _activeSpawnCoroutines.Clear();
        _activeMonsters.Clear();

        _isWaveActive = false;
    }

    public void OnMonsterDeath(GameObject monster)
    {
        if (_activeMonsters.Remove(monster))
        {
            RemainingMonsters--;
        }
    }

    public void RegisterSpawnedMonster(GameObject monster)
    {
        if (monster == null || !_isWaveActive)
        {
            return;
        }

        if (!_activeMonsters.Contains(monster))
        {
            _activeMonsters.Add(monster);
            _totalMonstersInWave++;
            RemainingMonsters++;
        }
    }

    public void KillRandomMonster()
    {
        if (_activeMonsters.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, _activeMonsters.Count);
        var monster = _activeMonsters[randomIndex];

        if (monster != null)
        {
            _activeMonsters.RemoveAt(randomIndex);
            MonsterPool.Instance?.Despawn(monster);
            RemainingMonsters--;
        }
    }

    private void OnAllMonstersDefeated()
    {
        _isWaveActive = false;
        _activeSpawnCoroutines.Clear();


        OnWaveCleared?.Invoke(_currentWaveId);
        OnWaveEnd?.Invoke(_currentWaveId);

        // 다음 웨이브가 없으면 게임 클리어 (20 웨이브 포함)
        if (!HasNextWave())
        {
            OnAllWavesCleared?.Invoke();
            GameStateManager.Instance?.EndGame(true);
        }
    }

    public void FailWave()
    {
        if (!_isWaveActive) return;

        _isWaveActive = false;
        _activeSpawnCoroutines.Clear();
        _activeMonsters.Clear();

        OnWaveFailed?.Invoke(_currentWaveId);
        OnWaveEnd?.Invoke(_currentWaveId);
    }

    private IEnumerator SpawnGroup(WaveData data)
    {
        if (data.PortalDelay > 0)
        {
            yield return new WaitForSeconds(data.PortalDelay);
        }

        if (!_portalDict.TryGetValue(data.PortalId, out Portal portal))
        {
            Debug.LogError($"[WaveManager] Portal {data.PortalId} not found!");
            yield break;
        }

        for (int i = 0; i < data.Count; i++)
        {
            // 포탈의 로컬 방향 기준으로 오프셋 적용
            Vector3 spawnPos = portal.transform.position + portal.transform.TransformDirection(_spawnOffset);

            // 스폰 이펙트 재생
            SpawnEffectPool.Instance?.Spawn(spawnPos);
            
            // 이펙트 후 딜레이
            yield return _effectToSpawnDelayWait;
            
            var enemy = MonsterPool.Instance.Spawn(data.EnemyType, spawnPos, Quaternion.identity, _currentStatMultiplier);
            if (enemy != null)
            {
                _activeMonsters.Add(enemy);
            }

            if (i < data.Count - 1)
            {
                yield return _spawnIntervalWait;
            }
        }
    }

    public bool HasNextWave()
    {
        // 최대 웨이브 도달 시 종료
        if (_currentWaveId >= _maxWave)
            return false;
        
        // 무한 웨이브 모드 (11~20)
        if (_currentWaveId >= _lastDesignedWave)
            return true;

        return DataTableManager.Instance.HasWaveData(_currentWaveId + 1);
    }
}

[Serializable]
public class InfiniteWaveEnemyPool
{
    [Tooltip("적 타입 ID (Addressable 주소와 일치해야 함)")]
    public string EnemyType;

    [Tooltip("기본 등장 가중치 (높을수록 자주 등장)")]
    public float BaseWeight = 1f;

    [Tooltip("웨이브당 가중치 증가량 (음수면 감소)")]
    public float WeightIncreasePerWave = 0f;

    [Tooltip("등장 시작 웨이브 (11 = 무한웨이브 첫 웨이브)")]
    public int MinWaveToAppear = 11;
}
