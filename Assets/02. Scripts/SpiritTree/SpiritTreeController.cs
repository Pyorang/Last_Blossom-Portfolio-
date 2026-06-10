using System;
using UnityEngine;
using LastBlossom.SpiritTree.Visual;

[RequireComponent(typeof(HealthComponent))]
public class SpiritTreeController : MonoBehaviour
{
    private const float DefaultMaxHP = 3000f;
    private const float DefaultWarningThreshold = 0.5f;
    private const float DefaultDangerThreshold = 0.25f;
    
    [Header("보호막 비주얼")]
    [SerializeField]
    [Tooltip("보호막 시각 효과 컨트롤러 (Bubble Shield 프리팹에 부착)")]
    private ShieldVisualController _shieldVisual;

    [Header("사운드")]
    [SerializeField] private string _hitSoundName = "영목_피격시";
    
    private HealthComponent _healthComponent;
    
    private SpiritTreeStatsModel _cachedStats;
    private bool _isStatsLoaded;
    
    public event Action<float, float, float> OnSpiritTreeDamaged;
    public event Action OnSpiritTreeDestroyed;
    public event Action<float, float, SpiritTreeHealthState> OnHealthChanged;
    public event Action<SpiritTreeHealthState> OnHealthStateChanged;
    public event Action<float, float> OnSpiritTreeHealed;

    private SpiritTreeHealthState _previousHealthState = SpiritTreeHealthState.Safe;
    
    public float CurrentHP => _healthComponent != null ? _healthComponent.CurrentHP : 0f;
    public float MaxHP => _healthComponent != null ? _healthComponent.MaxHP : GetMaxHP();
    public bool IsDestroyed => _healthComponent != null && _healthComponent.IsDead;
    public float HPRatio => _healthComponent != null ? _healthComponent.HPRatio : 0f;
    public SpiritTreeHealthState CurrentHealthState => GetCurrentHealthState();


    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
        TryFindShieldVisual();
    }

    private void Start()
    {
        LoadStatsAndInitialize();
        SubscribeToWaveEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDataTableManager();
        UnsubscribeFromHealthEvents();
        UnsubscribeShieldVisualFromEvents();
        UnsubscribeFromWaveEvents();
    }
    

    private void LoadStatsAndInitialize()
    {
        if (DataTableManager.Instance != null && DataTableManager.Instance.IsInitialized)
        {
            OnDataTableReady();
        }
        else if (DataTableManager.Instance != null)
        {
            DataTableManager.Instance.OnInitialized += OnDataTableReady;
        }
        else
        {
            Debug.LogWarning("[SpiritTreeController] DataTableManager를 찾을 수 없습니다. 기본값을 사용합니다.");
            InitializeWithDefaultValues();
        }
    }
    
    private void OnDataTableReady()
    {
        if (DataTableManager.Instance != null)
        {
            DataTableManager.Instance.OnInitialized -= OnDataTableReady;
        }

        var stats = DataTableManager.Instance.GetSpiritTreeStats();
        if (!string.IsNullOrEmpty(stats.ID))
        {
            _cachedStats = stats;
            _isStatsLoaded = true;
        }
        else
        {
            Debug.LogWarning("[SpiritTreeController] CSV에서 영목 데이터를 찾을 수 없습니다. 기본값을 사용합니다.");
        }
        
        InitializeHealth();
        SubscribeToHealthEvents();
        SubscribeShieldVisualToEvents();
        
        _previousHealthState = GetCurrentHealthState();
        
        OnHealthChanged?.Invoke(_healthComponent.CurrentHP, _healthComponent.MaxHP, _previousHealthState);
        
    }
    
    private void InitializeWithDefaultValues()
    {
        _isStatsLoaded = false;
        
        InitializeHealth();
        SubscribeToHealthEvents();
        SubscribeShieldVisualToEvents();
        
        _previousHealthState = GetCurrentHealthState();
        
        OnHealthChanged?.Invoke(_healthComponent.CurrentHP, _healthComponent.MaxHP, _previousHealthState);
        
    }
    
    private void InitializeHealth()
    {
        float maxHP = GetMaxHP();
        _healthComponent.Initialize(maxHP);
    }
    
    private float GetMaxHP()
    {
        return _isStatsLoaded ? _cachedStats.MaxHP : DefaultMaxHP;
    }
    public void RestoreFullHealth()
    {
        if (_healthComponent == null || _healthComponent.IsDead)
        {
            return;
        }
        
        float previousHP = _healthComponent.CurrentHP;
        float healAmount = _healthComponent.MaxHP - _healthComponent.CurrentHP;
        
        if (healAmount > 0)
        {
            _healthComponent.Heal(healAmount);
            OnSpiritTreeHealed?.Invoke(healAmount, _healthComponent.CurrentHP);
            
            _shieldVisual?.RestoreShield();
            
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (_healthComponent == null || _healthComponent.IsDead)
        {
            return;
        }
        
        _healthComponent.TakeDamage(damage);
    }
    
    public void Heal(float amount)
    {
        if (_healthComponent == null || _healthComponent.IsDead)
        {
            return;
        }
        
        float previousHP = _healthComponent.CurrentHP;
        _healthComponent.Heal(amount);
        
        float actualHeal = _healthComponent.CurrentHP - previousHP;
        if (actualHeal > 0)
        {
            OnSpiritTreeHealed?.Invoke(actualHeal, _healthComponent.CurrentHP);
        }
    }
    
    public void ResetSpiritTree()
    {
        if (_healthComponent == null)
        {
            return;
        }
        
        if (_healthComponent.IsDead)
        {
            _healthComponent.Revive(1f);
        }
        else
        {
            InitializeHealth();
        }
        
        _previousHealthState = SpiritTreeHealthState.Safe;
        
        _shieldVisual?.RestoreShield();
        
    }
    
    public Color GetHealthColor()
    {
        SpiritTreeHealthState state = GetCurrentHealthState();
        return state switch
        {
            SpiritTreeHealthState.Danger => Color.red,
            SpiritTreeHealthState.Warning => Color.yellow,
            _ => Color.green
        };
    }

    private SpiritTreeHealthState GetCurrentHealthState()
    {
        return HPRatio switch
        {
            <= 0f => SpiritTreeHealthState.Destroyed,
            <= DefaultDangerThreshold => SpiritTreeHealthState.Danger,
            <= DefaultWarningThreshold => SpiritTreeHealthState.Warning,
            _ => SpiritTreeHealthState.Safe
        };
    }

    private void CheckHealthStateChange()
    {
        SpiritTreeHealthState currentState = GetCurrentHealthState();
        
        if (currentState != _previousHealthState)
        {
            _previousHealthState = currentState;
            OnHealthStateChanged?.Invoke(currentState);
        }
    }
    

    private void SubscribeToHealthEvents()
    {
        if (_healthComponent == null)
        {
            return;
        }
        
        _healthComponent.OnHealthChanged += HandleHealthChanged;
        _healthComponent.OnDamageTaken += HandleDamageTaken;
        _healthComponent.OnDeath += HandleDeath;
    }
    
    private void UnsubscribeFromHealthEvents()
    {
        if (_healthComponent == null)
        {
            return;
        }
        
        _healthComponent.OnHealthChanged -= HandleHealthChanged;
        _healthComponent.OnDamageTaken -= HandleDamageTaken;
        _healthComponent.OnDeath -= HandleDeath;
    }
    

    private void SubscribeShieldVisualToEvents()
    {
        if (_shieldVisual == null)
        {
            return;
        }
        
        OnSpiritTreeDamaged += HandleShieldDamageFlash;
        OnSpiritTreeDestroyed += HandleShieldDissolve;
    }
    
    private void UnsubscribeShieldVisualFromEvents()
    {
        OnSpiritTreeDamaged -= HandleShieldDamageFlash;
        OnSpiritTreeDestroyed -= HandleShieldDissolve;
    }
    
    private void HandleShieldDamageFlash(float damage, float currentHP, float maxHP)
    {
        _shieldVisual?.PlayDamageFlash();
    }
    
    private void HandleShieldDissolve()
    {
        _shieldVisual?.PlayDissolveEffect();
    }
    

    private void HandleHealthChanged(float currentHP, float maxHP)
    {
        SpiritTreeHealthState state = GetCurrentHealthState();
        OnHealthChanged?.Invoke(currentHP, maxHP, state);
        CheckHealthStateChange();
    }
    
    private void HandleDamageTaken(float damage)
    {
        OnSpiritTreeDamaged?.Invoke(damage, _healthComponent.CurrentHP, _healthComponent.MaxHP);
        PlayHitSound();
    }

    private void PlayHitSound()
    {
        if (string.IsNullOrEmpty(_hitSoundName)) return;
        AudioManager.Instance?.PlaySFX3D(_hitSoundName, transform.position);
    }
    
    private void HandleDeath()
    {
        OnSpiritTreeDestroyed?.Invoke();
        GameStateManager.Instance?.EndGame(false);
    }
    

    private void TryFindShieldVisual()
    {
        if (_shieldVisual == null)
        {
            _shieldVisual = GetComponentInChildren<ShieldVisualController>();
            
            if (_shieldVisual != null)
            {
            }
        }
    }

    private void UnsubscribeFromDataTableManager()
    {
        if (DataTableManager.Instance != null)
        {
            DataTableManager.Instance.OnInitialized -= OnDataTableReady;
        }
    }

    private void SubscribeToWaveEvents()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveCleared += HandleWaveCleared;
        }
    }

    private void UnsubscribeFromWaveEvents()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveCleared -= HandleWaveCleared;
        }
    }

    private void HandleWaveCleared(int waveId)
    {
        RestoreFullHealth();
    }
}
