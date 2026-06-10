using System;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyAttack), typeof(HealthComponent))]
public class EnemyController : MonoBehaviour, IDeathAnimationEndNotifier
{
    [Header("데이터 설정")]
    [SerializeField] private string _enemyId = "Melee";
    
    [Header("컴포넌트 참조")]
    [SerializeField] private EnemyAnimator _animator;
    [SerializeField] private EnemyDissolveEffect _dissolveEffect;

    private EnemyMovement _movement;
    private EnemyAttack _attack;
    private HealthComponent _healthComponent;
    
    private EnemyStatsModel _stats;
    private EnemyState _currentState = EnemyState.Chase;
    private EnemyState _previousState = EnemyState.Chase;
    private Collider _collider;
    private bool _isInitialized;
    private bool _isSubscribedToGameManager;
    private bool _isDeathProcessed;
    private bool _isLockedToSpiritTree;
    private bool _isAggroedByDamage;
    private float _statMultiplier = 1f;
    
    public float StatMultiplier
    {
        get => _statMultiplier;
        set => _statMultiplier = value;
    }
    
    public void ReapplyHealth()
    {
        if (_healthComponent != null && _isInitialized)
        {
            float scaledHp = _stats.MaxHP * _statMultiplier;
            _healthComponent.Initialize(scaledHp);
            Debug.Log($"[{name}] ReapplyHealth: {_stats.MaxHP} * {_statMultiplier} = {scaledHp}");
        }
    }
    
    public string EnemyId => _enemyId;
    public EnemyType EnemyType => _isInitialized ? _stats.GetEnemyType() : EnemyType.Melee;
    public bool IsMeleeType => EnemyType == EnemyType.Melee || EnemyType == EnemyType.Tank;
    public EnemyStatsModel Stats => _stats;
    public EnemyMovement Movement => _movement;
    public EnemyAnimator Animator => _animator;
    public EnemyAttack Attack => _attack;
    public EnemyState CurrentState => _currentState;
    public bool IsInitialized => _isInitialized;
    public bool IsDead => _currentState == EnemyState.Dead;
    public bool IsLockedToSpiritTree => _isLockedToSpiritTree;
    public bool IsAggroedByDamage => _isAggroedByDamage;
    
    public event Action<EnemyState> OnStateChanged;
    public event Action OnDeathAnimationEnd;
    
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
            ResetHealth();
            ResetCollider();
            EnterState(_currentState);
        }
    }
    
    private void OnDisable()
    {
        UnsubscribeEvents();
        UnsubscribeFromGameManager();
        
        // 슬롯 반납
        if (IsMeleeType)
        {
            MeleeAttackCoordinator.Instance?.ReleaseSlot(this);
        }
    }

    private void Start()
    {
        if (!_isInitialized)
        {
            LoadStatsFromTable();
        }
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }
        
        if (_currentState == EnemyState.Idle || 
            _currentState == EnemyState.Hit || 
            _currentState == EnemyState.Dead)
        {
            return;
        }
        
        UpdateStateMachine();
    }

    private void ResetState()
    {
        _isDeathProcessed = false;
        _currentState = EnemyState.Chase;
        _previousState = EnemyState.Chase;
        _isLockedToSpiritTree = false;
        _isAggroedByDamage = false;
    }

    private void ResetHealth()
    {
        _healthComponent?.Initialize(_stats.MaxHP);
    }

    private void ResetCollider()
    {
        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }
    
    private void CacheComponents()
    {
        _movement ??= GetComponent<EnemyMovement>();
        _animator ??= GetComponentInChildren<EnemyAnimator>();
        _attack ??= GetComponent<EnemyAttack>();
        _healthComponent ??= GetComponent<HealthComponent>();
        _dissolveEffect ??= GetComponent<EnemyDissolveEffect>();
        _collider ??= GetComponent<Collider>();
    }
    
    private void SubscribeEvents()
    {
        if (_attack != null)
        {
            _attack.OnAttackEnd += OnAttackEnded;
        }
        
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageTaken += OnDamageTaken;
            _healthComponent.OnDeath += OnDeath;
        }
    }
    
    private void UnsubscribeEvents()
    {
        if (_attack != null)
        {
            _attack.OnAttackEnd -= OnAttackEnded;
        }
        
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageTaken -= OnDamageTaken;
            _healthComponent.OnDeath -= OnDeath;
        }
    }

    private void TrySubscribeToGameManager()
    {
        if (_isSubscribedToGameManager)
        {
            return;
        }
        
        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        _isSubscribedToGameManager = true;

        if (GameStateManager.Instance.IsGameOver)
        {
            ChangeState(EnemyState.Idle);
        }
    }

    private void UnsubscribeFromGameManager()
    {
        if (!_isSubscribedToGameManager)
        {
            return;
        }
        
        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        _isSubscribedToGameManager = false;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            ChangeState(EnemyState.Idle);
        }
    }
    
    private void LoadStatsFromTable()
    {
        if (DataTableManager.Instance == null)
        {
            return;
        }
        
        if (!DataTableManager.Instance.IsInitialized)
        {
            DataTableManager.Instance.OnInitialized += OnDataTableInitialized;
            return;
        }
        
        ApplyStatsFromTable();
    }
    
    private void OnDataTableInitialized()
    {
        DataTableManager.Instance.OnInitialized -= OnDataTableInitialized;
        ApplyStatsFromTable();
    }
    
    private void ApplyStatsFromTable()
    {
        if (!DataTableManager.Instance.TryGetEnemyStats(_enemyId, out var stats))
        {
            return;
        }
        
        _stats = stats;
        ApplyStatsToComponents();
        ConfigureAggroBehavior();
        _isInitialized = true;
        EnterState(_currentState);
    }
    
    private void ApplyStatsToComponents()
    {
        _movement?.ApplyStats(_stats);
        _attack?.ApplyStats(_stats);
        _healthComponent?.Initialize(_stats.MaxHP * _statMultiplier);
    }

    private void ConfigureAggroBehavior()
    {
        if (_movement == null)
        {
            return;
        }

        var behavior = AggroBehaviorFactory.Create(_stats.GetEnemyType());
        _movement.SetAggroBehavior(behavior);
        _movement.SetAttackingProvider(() => _attack != null && _attack.IsAttacking);
    }
    
    private void UpdateStateMachine()
    {
        switch (_currentState)
        {
            case EnemyState.Chase:
                UpdateChaseState();
                break;
        }
    }
    
    private void UpdateChaseState()
    {
        if (_movement.CurrentTarget == null)
        {
            return;
        }
        
        if (_movement.HasReachedTarget)
        {
            ChangeState(EnemyState.Attack);
        }
    }
    
    private void OnAttackEnded()
    {
        if (_currentState != EnemyState.Attack)
        {
            return;
        }
        
        if (_movement.IsTargetOutOfRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void OnDamageTaken(float damage)
    {
        if (_currentState == EnemyState.Dead)
        {
            return;
        }
        
        _isAggroedByDamage = true;
        
        // 영목 록온 상태의 근접 몬스터면 대기열에 추가
        if (IsMeleeType && _isLockedToSpiritTree)
        {
            MeleeAttackCoordinator.Instance?.EnqueueWaiting(this);
        }
        
        InterruptCurrentAction();
        
        _previousState = _currentState;
        ChangeState(EnemyState.Hit);
        
        _animator?.PlayHurt();
    }

    private void OnDeath()
    {
        if (_isDeathProcessed)
        {
            return;
        }
        _isDeathProcessed = true;
        
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        InterruptCurrentAction();
        ChangeState(EnemyState.Dead);
        
        _animator?.PlayDie();
    }

    private void InterruptCurrentAction()
    {
        _attack?.ForceEndAttack();
        _movement?.Stop();
    }

    public void OnHitAnimationComplete()
    {
        if (_currentState != EnemyState.Hit)
        {
            return;
        }
        
        if (_healthComponent != null && _healthComponent.IsDead)
        {
            return;
        }
        
        ReturnToPreviousState();
    }

    public void OnDeathAnimationComplete()
    {
        if (_currentState != EnemyState.Dead)
        {
            return;
        }
        
        OnDeathAnimationEnd?.Invoke();
        
        if (_dissolveEffect == null)
        {
            gameObject.SetActive(false);
        }
    }

    private void ReturnToPreviousState()
    {
        EnemyState targetState = _previousState;
        
        if (targetState == EnemyState.Hit || targetState == EnemyState.Dead)
        {
            targetState = EnemyState.Chase;
        }
        
        ChangeState(targetState);
    }
    
    private void ChangeState(EnemyState newState)
    {
        if (_currentState == newState)
        {
            return;
        }
        
        if (_currentState == EnemyState.Dead)
        {
            return;
        }
        
        _currentState = newState;
        EnterState(newState);
        OnStateChanged?.Invoke(newState);
    }
    
    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                _attack?.ForceEndAttack();
                break;
                
            case EnemyState.Chase:
                _movement?.SetMovementEnabled(true);
                _attack?.SetEnabled(false);
                break;
                
            case EnemyState.Attack:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(true);

                if (_movement?.CurrentTarget != null)
                {
                    var spiritTree = _movement.CurrentTarget.GetComponent<SpiritTreeController>();
                    if (spiritTree != null)
                    {
                        _isLockedToSpiritTree = true;
                        // 영목 공격 시 슬롯 반납
                        if (IsMeleeType)
                        {
                            MeleeAttackCoordinator.Instance?.ReleaseSlot(this);
                        }
                    }
                }
                break;
                
            case EnemyState.Hit:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                break;
                
            case EnemyState.Dead:
                _movement?.SetMovementEnabled(false);
                _movement?.DisableNavAgent();
                _attack?.SetEnabled(false);
                // 슬롯 반납
                if (IsMeleeType)
                {
                    MeleeAttackCoordinator.Instance?.ReleaseSlot(this);
                }
                break;
        }
    }
    
    public void ClearDamageAggro()
    {
        _isAggroedByDamage = false;
    }


    /// <summary>
    /// 강제로 영목 타겟으로 전환 (슬롯 없을 때)
    /// </summary>
    public void ForceTargetToSpiritTree()
    {
        _isLockedToSpiritTree = true;
        _isAggroedByDamage = false;
        _movement?.SetTargetToSpiritTree();
    }

    /// <summary>
    /// 대기열에서 승격되어 플레이어 타겟으로 전환
    /// </summary>
    public void OnPromotedToPlayerTarget()
    {
        _isLockedToSpiritTree = false;
        // AggroBehavior가 다음 Update에서 플레이어로 타겟 변경
    }
}
