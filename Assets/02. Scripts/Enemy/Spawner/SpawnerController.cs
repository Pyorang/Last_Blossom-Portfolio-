using System;
using UnityEngine;

[RequireComponent(typeof(SpawnerMovement), typeof(HealthComponent))]
public class SpawnerController : MonoBehaviour, IDeathAnimationEndNotifier
{
    private const float InitialSpawnTimer = 0f;
    private const float SpawnOffset = 2f;
    private const float DirectionSqrMagnitudeThreshold = 0.001f;
    private const float VerticalZero = 0f;

    [Header("데이터 설정")]
    [SerializeField] private string _enemyId = "Spawner";
    [SerializeField] private string _summonEnemyId = "Kamikaze";

    [Header("소환 설정")]
    [SerializeField] private Transform _mouthTransform;

    [Header("컴포넌트 참조")]
    [SerializeField] private SpawnerAnimator _animator;
    [SerializeField] private EnemyDissolveEffect _dissolveEffect;

    private SpawnerMovement _movement;
    private HealthComponent _health;
    private Collider _collider;
    private Transform _spiritTree;

    private SpawnerState _currentState = SpawnerState.Move;
    private SpawnerState _stateBeforeHurt = SpawnerState.Move;
    private float _spawnInterval;
    private float _spawnTimer;
    private float _maxHp;
    private bool _isInitialized;
    private bool _isSubscribedToGameManager;
    private bool _isDeathProcessed;
    private bool _pendingSpawnAfterHurt;
    private bool _isSpawnAnimationPlaying;
    private bool _hasSpawnedThisCycle;
    private bool _isHurtAnimationPlaying;
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

    public SpawnerState CurrentState => _currentState;
    public bool IsInitialized => _isInitialized;

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
            _health?.Initialize(_maxHp);
            ResetCollider();
            FindSpiritTree();
            _movement?.SetTarget(_spiritTree);
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

        if (_currentState == SpawnerState.Idle || 
            _currentState == SpawnerState.Dead ||
            _currentState == SpawnerState.Hurt)
        {
            return;
        }

        UpdateStateMachine();
    }

    private void CacheComponents()
    {
        _movement = GetComponent<SpawnerMovement>();
        _health = GetComponent<HealthComponent>();
        _animator ??= GetComponentInChildren<SpawnerAnimator>();
        _dissolveEffect ??= GetComponent<EnemyDissolveEffect>();
        _collider ??= GetComponent<Collider>();
    }

    private void ResetState()
    {
        _isDeathProcessed = false;
        _currentState = SpawnerState.Move;
        _stateBeforeHurt = SpawnerState.Move;
        _spawnTimer = InitialSpawnTimer;
        _pendingSpawnAfterHurt = false;
        _isSpawnAnimationPlaying = false;
        _hasSpawnedThisCycle = false;
        _isHurtAnimationPlaying = false;
    }

    private void ResetCollider()
    {
        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }

    private void SubscribeEvents()
    {
        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
            _health.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
            _health.OnDamageTaken -= HandleDamageTaken;
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

        GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _isSubscribedToGameManager = true;

        if (GameStateManager.Instance.IsGameOver)
        {
            ChangeState(SpawnerState.Idle);
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

        GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _isSubscribedToGameManager = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            ChangeState(SpawnerState.Idle);
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
        if (!DataTableManager.Instance.TryGetEnemyStats(_enemyId, out var stats))
        {
            return;
        }

        _maxHp = stats.MaxHP * _statMultiplier;
        _spawnInterval = stats.AttackCooldown;

        _movement?.ApplyStats(stats);
        _health?.Initialize(_maxHp);

        FindSpiritTree();
        _movement?.SetTarget(_spiritTree);

        _isInitialized = true;
        EnterState(_currentState);
    }

    private void FindSpiritTree()
    {
        // MonsterPool에서 SetTargets()로 주입받음
        // Find 제거됨
    }

    public void SetTargets(Transform spiritTree)
    {
        _spiritTree = spiritTree;
        _movement?.SetTarget(_spiritTree);
    }

    private void UpdateStateMachine()
    {
        if (_currentState == SpawnerState.Move)
        {
            UpdateMoveState();
        }
        else if (_currentState == SpawnerState.Spawn)
        {
            UpdateSpawnState();
        }
    }

    private void UpdateMoveState()
    {
        if (_movement == null || !_movement.HasReachedTarget)
        {
            return;
        }

        ChangeState(SpawnerState.Spawn);
    }

    private void UpdateSpawnState()
    {
        if (_isSpawnAnimationPlaying)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            ExecuteSpawn();
        }
    }

    private void ExecuteSpawn()
    {
        _isSpawnAnimationPlaying = true;
        _hasSpawnedThisCycle = false;
        _animator?.PlaySpawn();
    }

    public void OnSpawnAnimationHit()
    {
        if (_hasSpawnedThisCycle)
        {
            return;
        }

        _hasSpawnedThisCycle = true;
        SpawnKamikaze();
    }

    public void OnSpawnAnimationEnd()
    {
        _isSpawnAnimationPlaying = false;
        _spawnTimer = InitialSpawnTimer;
    }

    private void SpawnKamikaze()
    {
        if (_spiritTree == null)
        {
            FindSpiritTree();
            if (_spiritTree == null)
            {
                return;
            }
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        var kamikaze = MonsterPool.Instance?.Spawn(_summonEnemyId, spawnPosition, spawnRotation);

        if (kamikaze != null)
        {
            WaveManager.Instance?.RegisterSpawnedMonster(kamikaze);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (_mouthTransform != null)
        {
            return _mouthTransform.position;
        }

        return transform.position + transform.forward * SpawnOffset;
    }

    private Quaternion GetSpawnRotation()
    {
        if (_spiritTree == null)
        {
            return transform.rotation;
        }

        Vector3 directionToTree = _spiritTree.position - GetSpawnPosition();
        directionToTree.y = VerticalZero;

        if (directionToTree.sqrMagnitude < DirectionSqrMagnitudeThreshold)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(directionToTree.normalized);
    }

    private void HandleDamageTaken(float damage)
    {
        if (_currentState == SpawnerState.Dead)
        {
            return;
        }

        if (_isHurtAnimationPlaying)
        {
            return;
        }

        _stateBeforeHurt = _currentState;

        if (_currentState == SpawnerState.Spawn && _isSpawnAnimationPlaying)
        {
            if (_hasSpawnedThisCycle)
            {
                _spawnTimer = InitialSpawnTimer;
                _pendingSpawnAfterHurt = false;
            }
            else
            {
                _pendingSpawnAfterHurt = true;
            }
        }
        else
        {
            _pendingSpawnAfterHurt = false;
        }

        _isSpawnAnimationPlaying = false;
        _isHurtAnimationPlaying = true;
        ChangeState(SpawnerState.Hurt);
        _animator?.PlayHurt();
    }

    public void OnHurtAnimationComplete()
    {
        _isHurtAnimationPlaying = false;

        if (_currentState != SpawnerState.Hurt)
        {
            return;
        }

        if (_isDeathProcessed)
        {
            return;
        }

        if (_health != null && _health.IsDead)
        {
            HandleDeath();
            return;
        }

        if (_stateBeforeHurt == SpawnerState.Move && _movement != null && !_movement.HasReachedTarget)
        {
            ChangeState(SpawnerState.Move);
            return;
        }

        ChangeState(SpawnerState.Spawn);

        if (_pendingSpawnAfterHurt)
        {
            _pendingSpawnAfterHurt = false;
            ExecuteSpawn();
        }
    }

    private void HandleDeath()
    {
        if (_isDeathProcessed)
        {
            return;
        }

        _isDeathProcessed = true;
        _isSpawnAnimationPlaying = false;
        _isHurtAnimationPlaying = false;

        if (_collider != null)
        {
            _collider.enabled = false;
        }

        _movement?.Stop();

        _currentState = SpawnerState.Dead;
        EnterState(SpawnerState.Dead);

        _animator?.PlayDie();
    }

    public void OnDeathAnimationComplete()
    {
        if (_currentState != SpawnerState.Dead)
        {
            return;
        }

        OnDeathAnimationEnd?.Invoke();

        if (_dissolveEffect == null)
        {
            WaveManager.Instance?.OnMonsterDeath(gameObject);
            gameObject.SetActive(false);
        }
    }

    private void ChangeState(SpawnerState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        if (_currentState == SpawnerState.Dead)
        {
            return;
        }

        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(SpawnerState state)
    {
        switch (state)
        {
            case SpawnerState.Idle:
            case SpawnerState.Spawn:
            case SpawnerState.Hurt:
            case SpawnerState.Dead:
                _movement?.SetMovementEnabled(false);
                break;

            case SpawnerState.Move:
                _movement?.SetMovementEnabled(true);
                break;
        }
    }
}
