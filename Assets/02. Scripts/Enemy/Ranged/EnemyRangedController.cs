using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyRangedMovement), typeof(EnemyRangedAttack), typeof(HealthComponent))]
public class EnemyRangedController : MonoBehaviour, IDeathAnimationEndNotifier
{
    [Header("데이터 설정")]
    [SerializeField] private string _enemyId = "Ranged";

    [Header("컴포넌트 참조")]
    [SerializeField] private EnemyRangedAnimator _animator;
    [SerializeField] private EnemyDissolveEffect _dissolveEffect;
    [SerializeField] private Transform _modelTransform;

    [Header("낙하 설정")]
    [SerializeField] private float _fallDuration = 0.5f;
    [SerializeField] private AnimationCurve _fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private EnemyRangedMovement _movement;
    private EnemyRangedAttack _attack;
    private HealthComponent _health;
    private Collider _collider;

    private EnemyStatsModel _stats;
    private EnemyState _currentState = EnemyState.Chase;
    private EnemyState _previousState = EnemyState.Chase;
    private Vector3 _modelOriginalLocalPosition;
    private Coroutine _fallCoroutine;

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
        if (_health != null && _isInitialized)
        {
            float scaledHp = _stats.MaxHP * _statMultiplier;
            _health.Initialize(scaledHp);
            Debug.Log($"[{name}] ReapplyHealth: {_stats.MaxHP} * {_statMultiplier} = {scaledHp}");
        }
    }

    public EnemyStatsModel Stats => _stats;
    public EnemyRangedMovement Movement => _movement;
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
        CacheModelPosition();
    }

    private void OnEnable()
    {
        ResetState();
        ResetModelPosition();
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
        StopFallCoroutine();
    }

    private void Start()
    {
        if (!_isInitialized) LoadStatsFromTable();
    }

    private void Update()
    {
        if (!_isInitialized) return;
        if (_currentState == EnemyState.Idle || _currentState == EnemyState.Hit || _currentState == EnemyState.Dead) return;
        UpdateStateMachine();
    }

    private void CacheComponents()
    {
        _movement ??= GetComponent<EnemyRangedMovement>();
        _attack ??= GetComponent<EnemyRangedAttack>();
        _health ??= GetComponent<HealthComponent>();
        _animator ??= GetComponentInChildren<EnemyRangedAnimator>();
        _dissolveEffect ??= GetComponent<EnemyDissolveEffect>();
        _collider ??= GetComponent<Collider>();

        if (_modelTransform == null && _animator != null) _modelTransform = _animator.transform;
    }

    private void CacheModelPosition()
    {
        if (_modelTransform != null) _modelOriginalLocalPosition = _modelTransform.localPosition;
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
        _health?.Initialize(_stats.MaxHP);
    }

    private void ResetCollider()
    {
        if (_collider != null) _collider.enabled = true;
    }

    private void ResetModelPosition()
    {
        StopFallCoroutine();
        if (_modelTransform != null)
        {
            _modelTransform.localPosition = _modelOriginalLocalPosition;
            _modelTransform.localRotation = Quaternion.identity;
        }
    }

    private void StopFallCoroutine()
    {
        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }
    }

    private void SubscribeEvents()
    {
        if (_attack != null) _attack.OnAttackEnd += OnAttackEnded;
        if (_health != null)
        {
            _health.OnDamageTaken += OnDamageTaken;
            _health.OnDeath += OnDeath;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_attack != null) _attack.OnAttackEnd -= OnAttackEnded;
        if (_health != null)
        {
            _health.OnDamageTaken -= OnDamageTaken;
            _health.OnDeath -= OnDeath;
        }
    }

    private void TrySubscribeToGameManager()
    {
        if (_isSubscribedToGameManager) return;
        if (GameStateManager.Instance == null) return;

        GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        _isSubscribedToGameManager = true;

        if (GameStateManager.Instance.IsGameOver) ChangeState(EnemyState.Idle);
    }

    private void UnsubscribeFromGameManager()
    {
        if (!_isSubscribedToGameManager) return;
        if (GameStateManager.Instance == null) return;

        GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        _isSubscribedToGameManager = false;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver) ChangeState(EnemyState.Idle);
    }

    private void LoadStatsFromTable()
    {
        if (DataTableManager.Instance == null) return;

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
        if (!DataTableManager.Instance.TryGetEnemyStats(_enemyId, out var stats)) return;

        _stats = stats;
        _movement?.ApplyStats(_stats);
        _attack?.ApplyStats(_stats);
        _health?.Initialize(_stats.MaxHP * _statMultiplier);
        ConfigureAggroBehavior();
        _isInitialized = true;
        EnterState(_currentState);
    }

    private void ConfigureAggroBehavior()
    {
        if (_movement == null) return;

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
            case EnemyState.Attack:
                UpdateAttackState();
                break;
        }
    }

    private void UpdateChaseState()
    {
        if (_movement?.CurrentTarget == null) return;
        
        if (_movement.HasReachedTarget)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    private void UpdateAttackState()
    {
        if (_movement == null) return;
        
        // 공격 중이면 체크 안 함
        if (_attack != null && _attack.IsAttacking) return;
        
        // 타겟이 범위 밖이면 Chase로
        if (_movement.IsTargetOutOfRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void OnAttackEnded()
    {
        if (_currentState != EnemyState.Attack) return;
        
        if (_movement.IsTargetOutOfRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void OnDamageTaken(float damage)
    {
        if (_currentState == EnemyState.Dead || _isDeathProcessed) return;
        if (_health != null && _health.IsDead) return;

        _isAggroedByDamage = true;
        
        InterruptCurrentAction();
        _previousState = _currentState;
        ChangeState(EnemyState.Hit);
        _animator?.PlayHurt();
    }

    private void OnDeath()
    {
        if (_isDeathProcessed) return;
        _isDeathProcessed = true;

        if (_collider != null) _collider.enabled = false;

        InterruptCurrentAction();

        _currentState = EnemyState.Dead;
        EnterState(EnemyState.Dead);
        OnStateChanged?.Invoke(EnemyState.Dead);

        StartFallToGround();
        _animator?.PlayDie();
    }

    private void StartFallToGround()
    {
        StopFallCoroutine();
        if (_modelTransform == null) return;
        _fallCoroutine = StartCoroutine(FallToGroundCoroutine());
    }

    private IEnumerator FallToGroundCoroutine()
    {
        Vector3 startPosition = _modelTransform.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, 0f, startPosition.z);
        float elapsed = 0f;

        while (elapsed < _fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = _fallCurve.Evaluate(elapsed / _fallDuration);
            _modelTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        _modelTransform.localPosition = targetPosition;
        _fallCoroutine = null;
    }

    private void InterruptCurrentAction()
    {
        _attack?.ForceEndAttack();
        _movement?.Stop();
    }

    public void OnHitAnimationComplete()
    {
        if (_isDeathProcessed) return;
        if (_currentState != EnemyState.Hit) return;
        if (_health != null && _health.IsDead) return;
        ReturnToPreviousState();
    }

    public void OnDeathAnimationComplete()
    {
        if (_currentState != EnemyState.Dead) return;

        OnDeathAnimationEnd?.Invoke();

        if (_dissolveEffect == null)
        {
            WaveManager.Instance?.OnMonsterDeath(gameObject);
            gameObject.SetActive(false);
        }
    }

    private void ReturnToPreviousState()
    {
        EnemyState targetState = _previousState;
        if (targetState == EnemyState.Hit || targetState == EnemyState.Dead) targetState = EnemyState.Chase;
        ChangeState(targetState);
    }

    private void ChangeState(EnemyState newState)
    {
        if (_currentState == newState) return;
        if (_currentState == EnemyState.Dead) return;

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
                _isLockedToSpiritTree = false;
                break;

            case EnemyState.Attack:
                _movement?.SetMovementEnabled(true);  // 회전 계속 필요
                _attack?.SetEnabled(true);
                if (_movement?.CurrentTarget != null)
                {
                    var spiritTree = _movement.CurrentTarget.GetComponent<SpiritTreeController>();
                    if (spiritTree != null) _isLockedToSpiritTree = true;
                }
                break;

            case EnemyState.Hit:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                break;

            case EnemyState.Dead:
                _movement?.SetMovementEnabled(false);
                _attack?.SetEnabled(false);
                break;
        }
    }
    
    public void ClearDamageAggro()
    {
        _isAggroedByDamage = false;
    }
}
