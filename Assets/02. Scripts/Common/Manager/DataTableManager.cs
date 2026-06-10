using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DataTableManager : SingletonBehaviour<DataTableManager>
{
    private const string DATA_PATH = "Data";
    
    private Dictionary<string, CharacterStatsModel> _characterStatsTable;
    private SpiritTreeStatsModel _spiritTreeStats;
    private Dictionary<string, EnemyStatsModel> _enemyStatsTable;
    private Dictionary<string, SkillCoefficientModel> _skillCoefficientTable;
    private Dictionary<int, List<WaveData>> _waveTable;
    private List<WaveData> _allWaveData;
    private Dictionary<string, PerkDataModel> _perkTable;

    private List<AsyncOperationHandle<TextAsset>> _handles = new List<AsyncOperationHandle<TextAsset>>();

    public bool IsInitialized { get; private set; } = false;

    public event Action OnInitialized;

    protected override void Init()
    {
        base.Init();

        _characterStatsTable = new Dictionary<string, CharacterStatsModel>();
        _enemyStatsTable = new Dictionary<string, EnemyStatsModel>();
        _skillCoefficientTable = new Dictionary<string, SkillCoefficientModel>();
        _waveTable = new Dictionary<int, List<WaveData>>();
        _allWaveData = new List<WaveData>();
        _perkTable = new Dictionary<string, PerkDataModel>();

        LoadAllTablesAsync();
    }

    private void LoadAllTablesAsync()
    {
        int totalToLoad = 6;
        int loadedCount = 0;

        Action onLoaded = () =>
        {
            loadedCount++;
            if (loadedCount >= totalToLoad)
            {
                IsInitialized = true;
                OnInitialized?.Invoke();
            }
        };

        LoadTableAsync<CharacterStatsModel>("CharacterData", _characterStatsTable, item => item.ID, onLoaded);
        LoadSpiritTreeStatsAsync(onLoaded);
        LoadTableAsync<EnemyStatsModel>("EnemyStats", _enemyStatsTable, item => item.ID, onLoaded);
        LoadTableAsync<SkillCoefficientModel>("SkillCoefficient", _skillCoefficientTable, item => item.SkillId, onLoaded);
        LoadWaveTableAsync(onLoaded);
        LoadTableAsync<PerkDataModel>("PerkData", _perkTable, item => item.ID, onLoaded);
    }

    private void LoadTableAsync<T>(string fileName, Dictionary<string, T> table, Func<T, string> idSelector, Action onComplete) where T : struct
    {
        string address = $"{DATA_PATH}/{fileName}";
        var handle = Addressables.LoadAssetAsync<TextAsset>(address);
        
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var data = CsvParser.Parse<T>(op.Result.text);
                foreach (var item in data)
                {
                    table[idSelector(item)] = item;
                }
                _handles.Add(handle);
            }
            else
            {
                Debug.LogError($"[DataTableManager] {fileName} 로드 실패!");
            }
            
            onComplete?.Invoke();
        };
    }

    private void LoadSpiritTreeStatsAsync(Action onComplete)
    {
        string address = $"{DATA_PATH}/SpiritTreeStats";
        var handle = Addressables.LoadAssetAsync<TextAsset>(address);

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var data = CsvParser.Parse<SpiritTreeStatsModel>(op.Result.text);
                if (data.Length > 0)
                {
                    _spiritTreeStats = data[0];
                }
                _handles.Add(handle);

            }
            else
            {
                Debug.LogError("[DataTableManager] SpiritTreeStats 로드 실패!");
            }

            onComplete?.Invoke();
        };
    }

    private void LoadWaveTableAsync(Action onComplete)
    {
        string address = $"{DATA_PATH}/WaveData";
        var handle = Addressables.LoadAssetAsync<TextAsset>(address);

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var data = CsvParser.Parse<WaveData>(op.Result.text);
                foreach (var item in data)
                {
                    _allWaveData.Add(item);

                    if (!_waveTable.TryGetValue(item.WaveId, out var list))
                    {
                        list = new List<WaveData>();
                        _waveTable[item.WaveId] = list;
                    }
                    list.Add(item);
                }
                _handles.Add(handle);
            }
            else
            {
                Debug.LogError("[DataTableManager] WaveData 로드 실패!");
            }

            onComplete?.Invoke();
        };
    }

    private void OnDestroy()
    {
        foreach (var handle in _handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _handles.Clear();
    }

    #region CharacterStats

    public CharacterStatsModel GetCharacterStats(string id)
    {
        if (_characterStatsTable.TryGetValue(id, out var stats))
        {
            return stats;
        }

        Debug.LogWarning($"[DataTableManager] 캐릭터 스탯을 찾을 수 없음: {id}");
        return default;
    }

    public bool TryGetCharacterStats(string id, out CharacterStatsModel stats)
    {
        return _characterStatsTable.TryGetValue(id, out stats);
    }

    public bool HasCharacterStats(string id)
    {
        return _characterStatsTable.ContainsKey(id);
    }

    public string[] GetAllCharacterStatsIDs()
    {
        var keys = _characterStatsTable.Keys;
        string[] result = new string[keys.Count];
        keys.CopyTo(result, 0);
        return result;
    }

    public int GetCharacterStatsCount()
    {
        return _characterStatsTable.Count;
    }

    #endregion


    #region SpiritTreeStats

    public SpiritTreeStatsModel GetSpiritTreeStats() => _spiritTreeStats;

    #endregion


    #region EnemyStats

    public EnemyStatsModel GetEnemyStats(string id)
    {
        if (_enemyStatsTable.TryGetValue(id, out var stats))
        {
            return stats;
        }

        Debug.LogWarning($"[DataTableManager] 적 스탯을 찾을 수 없음: {id}");
        return default;
    }

    public bool TryGetEnemyStats(string id, out EnemyStatsModel stats)
    {
        return _enemyStatsTable.TryGetValue(id, out stats);
    }

    public bool HasEnemyStats(string id)
    {
        return _enemyStatsTable.ContainsKey(id);
    }

    public string[] GetAllEnemyStatsIDs()
    {
        var keys = _enemyStatsTable.Keys;
        string[] result = new string[keys.Count];
        keys.CopyTo(result, 0);
        return result;
    }

    public int GetEnemyStatsCount()
    {
        return _enemyStatsTable.Count;
    }

    #endregion


    #region SkillCoefficient

    public SkillCoefficientModel GetSkillCoefficient(string skillId)
    {
        if (_skillCoefficientTable.TryGetValue(skillId, out var model))
        {
            return model;
        }

        Debug.LogWarning($"[DataTableManager] 스킬 계수를 찾을 수 없음: {skillId}");
        return default;
    }

    public float GetCoefficient(string skillId)
    {
        if (_skillCoefficientTable.TryGetValue(skillId, out var model))
        {
            return model.Coefficient;
        }

        Debug.LogWarning($"[DataTableManager] 스킬 계수를 찾을 수 없음: {skillId}, 기본값 1 반환");
        return 1f;
    }

    public bool TryGetSkillCoefficient(string skillId, out SkillCoefficientModel model)
    {
        return _skillCoefficientTable.TryGetValue(skillId, out model);
    }

    #endregion


    #region WaveData

    public List<WaveData> GetWaveData(int waveId)
    {
        if (_waveTable.TryGetValue(waveId, out var list))
        {
            return list;
        }

        return null;
    }

    public bool HasWaveData(int waveId)
    {
        return _waveTable.ContainsKey(waveId);
    }

    public int GetTotalWaveCount()
    {
        return _waveTable.Count;
    }

    public List<WaveData> GetAllWaveData()
    {
        return _allWaveData;
    }

    public HashSet<string> GetEnemyTypesInWave(int waveId)
    {
        var result = new HashSet<string>();
        if (_waveTable.TryGetValue(waveId, out var list))
        {
            foreach (var data in list)
            {
                result.Add(data.EnemyType);
            }
        }
        return result;
    }

    #endregion


    #region PerkData

    public PerkDataModel GetPerkData(string id)
    {
        if (_perkTable.TryGetValue(id, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[DataTableManager] 퍽 데이터를 찾을 수 없음: {id}");
        return default;
    }

    public bool TryGetPerkData(string id, out PerkDataModel data)
    {
        return _perkTable.TryGetValue(id, out data);
    }

    public List<string> GetPerkIdsByWave(int waveId)
    {
        var result = new List<string>();
        foreach (var kvp in _perkTable)
        {
            if (kvp.Value.WaveId == waveId)
            {
                result.Add(kvp.Key);
            }
        }
        return result;
    }

    #endregion
}
