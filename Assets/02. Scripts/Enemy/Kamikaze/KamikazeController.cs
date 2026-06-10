using System;
using UnityEngine;

[RequireComponent(typeof(KamikazeMovement), typeof(HealthComponent))]
public class KamikazeController : MonoBehaviour, IDeathAnimationEndNotifier
{
    [Header("데이터 설정")]
    [SerializeField] private string _enemyId = "Kamikaze";

    [Header("폭발 설정")]
    [SerializeField] private float _explosionRadius = 3f;
    [SerializeField] private LayerMask _explosionLayerMask;

    [Header("컴포넌트 참조")]
    [SerializeField] private KamikazeAnimator _animator;
    [SerializeField] private KamikazeVisualFeedback _visualFeedback;
    [SerializeField] private EnemyDissolveEffect _dissolveEffect;
    [SerializeField] private EnemySFX _enemySFX;

    private KamikazeMovement _movement;
    private HealthComponent _health;
    private Collider _collider;

    private float _explosionDelay;
    private float _explosionDamage;
    private float _maxHp;
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

    private KamikazeState _currentState = KamikazeState.Chase;
    private float _explosionTimer;
    private bool _isInitialized;
    private bool _isSubscribedToGameManager;
    private bool _isDeathProcessed;

    public KamikazeState CurrentState => _currentState;

    public event Action OnDeathAnimationEnd;

    private static readonly Collider[] s_overlapBuffer = new Collider[32];

    private void Awake()
    {
        _movement = GetComponent<KamikazeMovement>();
        _health = GetComponent<HealthComponent>();
        _collider = GetComponent<Collider>();
        _animator ??= GetComponentInChildren<KamikazeAnimator>();
        _visualFeedback ??= GetComponentInChildren<KamikazeVisualFeedback>();
        _dissolveEffect ??= GetComponent<EnemyDissolveEffect>();
        _enemySFX ??= GetComponentInChildren<EnemySFX>();
    }

    private void OnEnable()
    {
        _isDeathProcessed = false;
        _currentState = KamikazeState.Chase;
        _explosionTimer = 0f;

        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
        }

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
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
        }

        if (_isSubscribedToGameManager && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            _isSubscribedToGameManager = false;
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
        if (!_isInitialized || _currentState == KamikazeState.Idle || _currentState == KamikazeState.Dead)
        {
            return;
        }

        if (_currentState == KamikazeState.Chase)
        {
            if (_movement != null && _movement.HasReachedTarget)
            {
                ChangeState(KamikazeState.Detonating);
            }
        }
        else if (_currentState == KamikazeState.Detonating)
        {
            _explosionTimer += Time.deltaTime;
            if (_explosionTimer >= _explosionDelay)
            {
                Explode();
            }
        }
    }

    private void TrySubscribeToGameManager()
    {
        if (_isSubscribedToGameManager || GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _isSubscribedToGameManager = true;

        if (GameStateManager.Instance.IsGameOver)
        {
            ChangeState(KamikazeState.Idle);
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            ChangeState(KamikazeState.Idle);
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

        _maxHp = stats.MaxHP * _statMultiplier;  // 배율 적용
        _explosionDamage = stats.AttackDamage;
        _explosionDelay = stats.AttackCooldown;

        _movement?.ApplyStats(stats);
        _health?.Initialize(_maxHp);

        _isInitialized = true;
        EnterState(_currentState);
    }

    private void Explode()
    {
        if (_currentState == KamikazeState.Dead)
        {
            return;
        }

        _enemySFX?.Play("자폭몬_폭발");
        _visualFeedback?.PlayExplosionEffect();
        DealExplosionDamage();
        Die(false);
    }

    private void DealExplosionDamage()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _explosionRadius, s_overlapBuffer, _explosionLayerMask.value);

        for (int i = 0; i < hitCount; i++)
        {
            var hitCollider = s_overlapBuffer[i];
            if (hitCollider == null || hitCollider.gameObject == gameObject)
            {
                continue;
            }

            IDamageable damageable;
            if (!hitCollider.TryGetComponent<IDamageable>(out damageable))
            {
                damageable = hitCollider.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(_explosionDamage);
            }
        }
    }

    private void HandleDeath()
    {
        if (!_isDeathProcessed)
        {
            Die(true);
        }
    }

    private void Die(bool playDeathAnimation)
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

        _movement?.Stop();
        _visualFeedback?.StopAllFeedback();

        _currentState = KamikazeState.Dead;
        _movement?.SetMovementEnabled(false);

        if (playDeathAnimation)
        {
            _animator?.PlayDie();
        }
        else
        {
            OnDeathComplete();
        }
    }

    public void OnDeathAnimationComplete()
    {
        if (_currentState == KamikazeState.Dead)
        {
            OnDeathComplete();
        }
    }

    private void OnDeathComplete()
    {
        OnDeathAnimationEnd?.Invoke();

        if (_dissolveEffect == null)
        {
            WaveManager.Instance?.OnMonsterDeath(gameObject);
            gameObject.SetActive(false);
        }
    }

    private void ChangeState(KamikazeState newState)
    {
        if (_currentState == newState || _currentState == KamikazeState.Dead)
        {
            return;
        }

        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(KamikazeState state)
    {
        switch (state)
        {
            case KamikazeState.Idle:
                _movement?.SetMovementEnabled(false);
                _visualFeedback?.StopAllFeedback();
                break;

            case KamikazeState.Chase:
                _movement?.SetMovementEnabled(true);
                _visualFeedback?.StartChaseFeedback();
                break;

            case KamikazeState.Detonating:
                _movement?.SetMovementEnabled(false);
                _explosionTimer = 0f;
                _visualFeedback?.StartDetonationFeedback(_explosionDelay);
                _animator?.PlayDetonate();
                break;

            case KamikazeState.Dead:
                _movement?.SetMovementEnabled(false);
                break;
        }
    }
}
