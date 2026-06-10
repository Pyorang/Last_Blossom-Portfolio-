using System;
using UnityEngine;

[RequireComponent(typeof(TankMovement))]
[RequireComponent(typeof(TankAttack))]
[RequireComponent(typeof(TankRoar))]
[RequireComponent(typeof(TankShield))]
[RequireComponent(typeof(HealthComponent))]
public class TankController : MonoBehaviour, IDeathAnimationEndNotifier
{
    private const float GroggyDuration = 3f;
    private const float GroggyHpThreshold = 0.5f;

    [SerializeField] private string _enemyId = "Tank";
    [SerializeField] private TankAnimator _animator;
    [SerializeField] private EnemyDissolveEffect _dissolveEffect;
    [SerializeField, Range(0f, 1f)] private float _shieldDamageReduction = 0.25f;

    private TankMovement _movement;
    private TankAttack _attack;
    private TankRoar _roar;
    private TankShield _shield;
    private HealthComponent _health;
    private Collider _collider;

    private TankState _currentState = TankState.Chase;
    private float _maxHp;
    private float _groggyTimer;
    private bool _isInitialized;
    private bool _isSubscribedToGameManager;
    private bool _isDeathProcessed;
    private bool _isAggroedByDamage;
    private bool _hasTriggeredGroggy;
    private bool _hasReachedSpiritTreeOnce;
    private float _statMultiplier = 1f;
    
    public float StatMultiplier
    {
        get => _statMultiplier;
        set => _statMultiplier = value;
    }
    
    public void ReapplyHealth()
    {
        if (_health != null && DataTableManager.Instance != null && 
            DataTableManager.Instance.TryGetEnemyStats(_enemyId, out var stats))
        {
            float scaledHp = stats.MaxHP * _statMultiplier;
            _maxHp = scaledHp;
            _health.Initialize(scaledHp);
            Debug.Log($"[{name}] ReapplyHealth: {stats.MaxHP} * {_statMultiplier} = {scaledHp}");
        }
    }

    public TankState CurrentState => _currentState;
    public bool IsInitialized => _isInitialized;
    public bool IsDead => _currentState == TankState.Dead;
    public bool IsAggroedByDamage => _isAggroedByDamage;
    public bool HasReachedSpiritTreeOnce => _hasReachedSpiritTreeOnce;
    public float CurrentHP => _health != null ? _health.CurrentHP : 0f;
    public float MaxHP => _health != null ? _health.MaxHP : _maxHp;

    public event Action OnDeathAnimationEnd;
    public event Action OnHitFeedback;

    public event Action<float, float> OnHealthChanged
    {
        add { if (_health != null) _health.OnHealthChanged += value; }
        remove { if (_health != null) _health.OnHealthChanged -= value; }
    }

    public event Action OnDeath
    {
        add { if (_health != null) _health.OnDeath += value; }
        remove { if (_health != null) _health.OnDeath -= value; }
    }

    public void Heal(float amount)
    {
        if (_currentState == TankState.Dead) return;
        _health?.Heal(amount);
    }

    public void MarkReachedSpiritTreeOnce()
    {
        _hasReachedSpiritTreeOnce = true;
    }

    public void ClearDamageAggro()
    {
        _isAggroedByDamage = false;
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        ResetState();
        SubscribeEvents();
        TrySubscribeToGameManager();

        if (_isInitialized)
        {
            _health?.Initialize(_maxHp);
            if (_collider != null) _collider.enabled = true;
            EnterState(_currentState);
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        UnsubscribeFromGameManager();
    }

    private void Start()
    {
        if (!_isInitialized) LoadStatsFromTable();
    }

    private void Update()
    {
        if (!_isInitialized) return;
        if (_currentState == TankState.Idle || _currentState == TankState.Dead) return;
        UpdateStateMachine();
    }

    private void CacheComponents()
    {
        _movement = GetComponent<TankMovement>();
        _attack = GetComponent<TankAttack>();
        _roar = GetComponent<TankRoar>();
        _shield = GetComponent<TankShield>();
        _health = GetComponent<HealthComponent>();
        _animator ??= GetComponentInChildren<TankAnimator>();
        _dissolveEffect ??= GetComponent<EnemyDissolveEffect>();
        _collider ??= GetComponent<Collider>();
    }

    private void ResetState()
    {
        _isDeathProcessed = false;
        _currentState = TankState.Chase;
        _isAggroedByDamage = false;
        _hasTriggeredGroggy = false;
        _groggyTimer = 0f;
        _hasReachedSpiritTreeOnce = false;
    }

    private void SubscribeEvents()
    {
        if (_attack != null) _attack.OnAttackEnd += HandleAttackEnd;
        if (_roar != null) _roar.OnRoarEnd += HandleRoarEnd;
        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnBeforeDamage += HandleBeforeDamage;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_attack != null) _attack.OnAttackEnd -= HandleAttackEnd;
        if (_roar != null) _roar.OnRoarEnd -= HandleRoarEnd;
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnBeforeDamage -= HandleBeforeDamage;
        }
    }

    private void TrySubscribeToGameManager()
    {
        if (_isSubscribedToGameManager) return;
        if (GameStateManager.Instance == null) return;

        GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _isSubscribedToGameManager = true;

        if (GameStateManager.Instance.IsGameOver) ChangeState(TankState.Idle);
    }

    private void UnsubscribeFromGameManager()
    {
        if (!_isSubscribedToGameManager) return;
        if (GameStateManager.Instance == null) return;

        GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _isSubscribedToGameManager = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver) ChangeState(TankState.Idle);
    }

    private void LoadStatsFromTable()
    {
        if (DataTableManager.Instance == null) return;

        if (!DataTableManager.Instance.IsInitialized)
        {
            DataTableManager.Instance.OnInitialized += HandleDataTableInitialized;
            return;
        }

        ApplyStatsFromTable();
    }

    private void HandleDataTableInitialized()
    {
        DataTableManager.Instance.OnInitialized -= HandleDataTableInitialized;
        ApplyStatsFromTable();
    }

    private void ApplyStatsFromTable()
    {
        if (!DataTableManager.Instance.TryGetEnemyStats(_enemyId, out EnemyStatsModel stats)) return;

        _maxHp = stats.MaxHP * _statMultiplier;
        _movement?.ApplyStats(stats);
        _attack?.ApplyStats(stats);
        _roar?.SetDamage(stats.AttackDamage);
        _health?.Initialize(_maxHp);
        // SetShowDamageText(false) 제거 - HealthComponent가 직접 데미지 텍스트 표시

        _isInitialized = true;
        EnterState(_currentState);
    }

    private void UpdateStateMachine()
    {
        switch (_currentState)
        {
            case TankState.Chase:
                UpdateChaseState();
                break;
            case TankState.Groggy:
                UpdateGroggyState();
                break;
        }
    }

    private void UpdateChaseState()
    {
        if (_movement == null) return;

        if (_movement.HasReachedTarget)
        {
            if (_movement.IsTargetingSpiritTree) MarkReachedSpiritTreeOnce();
            ChangeState(TankState.Attack);
            return;
        }

        if (_movement.IsTargetingPlayer)
        {
            _roar?.UpdateTimer(Time.deltaTime);
            if (_roar != null && _roar.IsReady()) ChangeState(TankState.Roar);
        }
    }

    private void UpdateGroggyState()
    {
        _groggyTimer -= Time.deltaTime;
        if (_groggyTimer <= 0f) ChangeState(TankState.Chase);
    }

    // 데미지 적용 전 실드 감소 처리
    private void HandleBeforeDamage(HealthComponent.DamageInfo damageInfo)
    {
        if (_currentState == TankState.Dead) return;

        bool isShielded = _shield != null && !_shield.IsShieldBroken;
        
        if (isShielded)
        {
            damageInfo.damage *= _shieldDamageReduction;
            damageInfo.isShielded = true;
        }
    }

    private void HandleDamageTaken(float damage)
    {
        if (_currentState == TankState.Dead) return;
        if (_health.CurrentHP <= 0f) return;

        _isAggroedByDamage = true;
        OnHitFeedback?.Invoke();

        if (!_hasTriggeredGroggy && _health.HPRatio <= GroggyHpThreshold)
        {
            TriggerGroggy();
        }
    }

    private void TriggerGroggy()
    {
        _hasTriggeredGroggy = true;
        _shield?.BreakShield();
        InterruptCurrentAction();
        ChangeState(TankState.Groggy);
    }

    private void HandleAttackEnd()
    {
        if (_currentState != TankState.Attack) return;

        if (_movement != null && _movement.IsTargetingPlayer && _roar != null && _roar.IsReady())
        {
            ChangeState(TankState.Roar);
            return;
        }

        if (_movement != null && _movement.IsTargetOutOfRange)
        {
            ChangeState(TankState.Chase);
        }
    }

    private void HandleRoarEnd()
    {
        if (_currentState != TankState.Roar) return;
        _animator?.ForceReturnToLocomotion();
        ChangeState(TankState.Chase);
    }

    private void HandleDeath()
    {
        if (_isDeathProcessed) return;
        _isDeathProcessed = true;

        if (_collider != null) _collider.enabled = false;

        _movement?.UnlockRotation();
        _movement?.SetRotationEnabled(false);

        InterruptCurrentAction();
        ChangeState(TankState.Dead);
        _animator?.PlayDie();
    }

    private void InterruptCurrentAction()
    {
        _attack?.ForceEndAttack();
        _roar?.ForceEndRoar();
        _movement?.Stop();
    }

    public void OnAttackHitFrame()
    {
        if (_movement != null && _movement.IsTargetingPlayer && _roar != null)
        {
            _roar.StartCooldownFromFirstPlayerAttack();
        }
        _attack?.OnAttackHitFrame();
    }

    public void OnAttackEndFrame()
    {
        _attack?.OnAttackEndFrame();
    }

    public void OnRoarHitFrame()
    {
        _roar?.OnRoarHitFrame();
    }

    public void OnRoarEndFrame()
    {
        _roar?.OnRoarEndFrame();
    }

    public void OnDeathAnimationComplete()
    {
        if (_currentState != TankState.Dead) return;

        OnDeathAnimationEnd?.Invoke();

        if (_dissolveEffect == null)
        {
            WaveManager.Instance?.OnMonsterDeath(gameObject);
            gameObject.SetActive(false);
        }
    }

    private void ChangeState(TankState newState)
    {
        if (_currentState == newState) return;
        if (_currentState == TankState.Dead) return;

        ExitState(_currentState);
        _currentState = newState;
        EnterState(newState);
    }

    private void ExitState(TankState state)
    {
        if (state == TankState.Groggy) _roar?.ResumeTimer();
    }

    private void EnterState(TankState state)
    {
        switch (state)
        {
            case TankState.Idle:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                _attack?.ForceEndAttack();
                _roar?.ForceEndRoar();
                break;

            case TankState.Chase:
                _movement?.SetMovementEnabled(true);
                _attack?.SetEnabled(false);
                break;

            case TankState.Attack:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(true);
                break;

            case TankState.Roar:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                if (_movement != null && _movement.IsTargetingPlayer)
                {
                    _roar?.StartRoar();
                }
                else
                {
                    ChangeState(TankState.Chase);
                }
                break;

            case TankState.Groggy:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                _roar?.PauseTimer();
                _groggyTimer = GroggyDuration;
                _animator?.PlayGroggy();
                break;

            case TankState.Dead:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                break;
        }
    }
}
