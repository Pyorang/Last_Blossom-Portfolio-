using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private float _playerStoppingDistance = 1.5f;
    [SerializeField] private float _spiritTreeStoppingDistance = 4.5f;
    
    [Header("어그로 설정")]
    [SerializeField] private float _aggroRange = 5f;
    [SerializeField] private float _aggroReleaseRange = 8f;
    
    private NavMeshAgent _navAgent;
    private EnemyController _controller;
    private Transform _currentTarget;
    private Transform _spiritTree;
    private Transform _player;
    private bool _isMovementEnabled = true;
    private bool _hasEverHadPath;
    private IAggroBehavior _aggroBehavior;
    private Func<bool> _isAttackingProvider;

    public bool IsMoving => _navAgent != null && _navAgent.enabled && _navAgent.velocity.magnitude / _moveSpeed > 0.1f;
    public Transform CurrentTarget => _currentTarget;
    
    public float DistanceToTarget
    {
        get
        {
            if (_currentTarget == null)
            {
                return float.MaxValue;
            }
            return Vector3.Distance(transform.position, _currentTarget.position);
        }
    }
    
    public float CurrentStoppingDistance => _currentTarget == _player 
        ? _playerStoppingDistance 
        : _spiritTreeStoppingDistance;
    
    public bool HasReachedTarget
    {
        get
        {
            if (_currentTarget == null || !_hasEverHadPath)
            {
                return false;
            }

            float distance = DistanceToTarget;
            float threshold = CurrentStoppingDistance;

            if (_navAgent != null && _navAgent.enabled && !_navAgent.pathPending)
            {
                if (_navAgent.hasPath && _navAgent.remainingDistance <= threshold + 0.1f)
                {
                    return true;
                }

                if (_navAgent.pathStatus == NavMeshPathStatus.PathPartial && distance <= threshold * 1.5f)
                {
                    return true;
                }
            }

            return distance <= threshold;
        }
    }

    public bool IsTargetOutOfRange => DistanceToTarget > CurrentStoppingDistance * 1.5f;
    
    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _controller = GetComponent<EnemyController>();
    }

    private void OnEnable()
    {
        _currentTarget = null;
        _hasEverHadPath = false;
        _isMovementEnabled = true;
        
        if (_navAgent != null)
        {
            _navAgent.enabled = true;
            _navAgent.Warp(transform.position);
        }
    }
    
    private void Start()
    {
        ConfigureNavAgent();
        FindTargetsIfNeeded();
        SetTarget(_spiritTree);
    }
    
    private void Update()
    {
        // 어그로 평가는 항상 수행 (Attack 상태에서도 타겟 변경 가능)
        EvaluateAggro();
        
        if (!_isMovementEnabled)
        {
            return;
        }
        
        MoveToTarget();
        TrackPathState();
    }

    private void TrackPathState()
    {
        if (_navAgent.hasPath)
        {
            _hasEverHadPath = true;
        }
    }
    
    public void SetAggroBehavior(IAggroBehavior behavior)
    {
        _aggroBehavior = behavior;
    }

    public void SetAttackingProvider(Func<bool> provider)
    {
        _isAttackingProvider = provider;
    }
    
    private void ConfigureNavAgent()
    {
        _navAgent.speed = _moveSpeed;
        _navAgent.angularSpeed = _rotationSpeed;
        _navAgent.updateRotation = true;
    }
    
    private void UpdateStoppingDistance()
    {
        _navAgent.stoppingDistance = CurrentStoppingDistance;
    }
    
    private void FindTargetsIfNeeded()
    {
        // MonsterPool에서 SetTargets()로 주입받음
        // Find 제거됨
    }

    public void SetTargets(Transform spiritTree, Transform player)
    {
        _spiritTree = spiritTree;
        _player = player;
    }
    
    private void EvaluateAggro()
    {
        if (_aggroBehavior == null)
        {
            return;
        }

        var context = new EnemyAggroContext
        {
            Self = transform,
            Player = _player,
            SpiritTree = _spiritTree,
            CurrentTarget = _currentTarget,
            AggroRange = _aggroRange,
            AggroReleaseRange = _aggroReleaseRange,
            IsAttacking = _isAttackingProvider?.Invoke() ?? false,
            IsLockedToSpiritTree = _controller != null && _controller.IsLockedToSpiritTree,
            IsAggroedByDamage = _controller != null && _controller.IsAggroedByDamage,
            ClearDamageAggro = _controller != null ? _controller.ClearDamageAggro : null,
            Controller = _controller
        };

        var newTarget = _aggroBehavior.EvaluateTarget(context);
        
        if (newTarget != _currentTarget)
        {
            SetTarget(newTarget);
        }
    }
    
    private void MoveToTarget()
    {
        if (_currentTarget == null)
        {
            SetTarget(_spiritTree);
            return;
        }

        if (!_navAgent.enabled || !_navAgent.isOnNavMesh)
        {
            return;
        }
        
        _navAgent.SetDestination(_currentTarget.position);
    }
    
    public void SetTarget(Transform target)
    {
        _currentTarget = target;
        _hasEverHadPath = false;
        UpdateStoppingDistance();
    }


    public void SetTargetToSpiritTree()
    {
        SetTarget(_spiritTree);
    }
    
    public void Stop()
    {
        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.velocity = Vector3.zero;
        }
    }
    
    public void SetMovementEnabled(bool enabled)
    {
        _isMovementEnabled = enabled;
        if (!enabled)
        {
            Stop();
        }
        else
        {
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = false;
            }
        }
    }
    
    public void DisableNavAgent()
    {
        if (_navAgent != null)
        {
            _navAgent.enabled = false;
        }
    }
    
    public void ApplyStats(EnemyStatsModel stats)
    {
        _moveSpeed = stats.MoveSpeed;
        _rotationSpeed = stats.RotationSpeed;
        _playerStoppingDistance = stats.PlayerStoppingDistance;
        _spiritTreeStoppingDistance = stats.SpiritTreeStoppingDistance;
        _aggroRange = stats.AggroRange;
        _aggroReleaseRange = stats.AggroReleaseRange;
        
        ConfigureNavAgent();
        UpdateStoppingDistance();
    }
}
