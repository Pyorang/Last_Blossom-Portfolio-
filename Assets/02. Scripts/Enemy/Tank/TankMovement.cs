using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TankMovement : MonoBehaviour
{
    private const float VelocityThreshold = 0.1f;
    private const float PathReachedBuffer = 0.1f;
    private const float PartialPathMultiplier = 1.5f;

    private NavMeshAgent _navAgent;
    private TankController _controller;
    private Transform _currentTarget;
    private Transform _spiritTree;
    private Transform _player;

    private float _moveSpeed;
    private float _rotationSpeed;
    private float _playerStoppingDistance;
    private float _spiritTreeStoppingDistance;
    private float _aggroRange;
    private float _aggroReleaseRange;

    private bool _isRotationLocked;
    private Quaternion _lockedRotation;
    private bool _isMovementEnabled = true;
    private bool _isRotationEnabled = true;

    public bool IsMoving => _navAgent != null && _navAgent.enabled && _moveSpeed > 0f &&
                            _navAgent.velocity.magnitude / _moveSpeed > VelocityThreshold;

    public Transform CurrentTarget => _currentTarget;
    public Transform Player => _player;

    public float DistanceToTarget => _currentTarget == null
        ? float.MaxValue
        : Vector3.Distance(transform.position, _currentTarget.position);

    private float CurrentStoppingDistance => _currentTarget == _player
        ? _playerStoppingDistance
        : _spiritTreeStoppingDistance;

    public bool HasReachedTarget
    {
        get
        {
            if (_currentTarget == null) return false;

            float threshold = CurrentStoppingDistance;

            if (_navAgent != null && _navAgent.enabled && !_navAgent.pathPending)
            {
                if (_navAgent.hasPath && _navAgent.remainingDistance <= threshold + PathReachedBuffer)
                    return true;

                if (_navAgent.pathStatus == NavMeshPathStatus.PathPartial &&
                    DistanceToTarget <= threshold * PartialPathMultiplier)
                    return true;
            }

            return DistanceToTarget <= threshold;
        }
    }

    public bool IsTargetOutOfRange => DistanceToTarget > CurrentStoppingDistance * PartialPathMultiplier;
    public bool IsTargetingPlayer => _currentTarget == _player;
    public bool IsTargetingSpiritTree => _currentTarget == _spiritTree;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _controller = GetComponent<TankController>();
    }

    private void OnEnable()
    {
        _currentTarget = null;
        _isMovementEnabled = true;

        if (_navAgent != null)
        {
            _navAgent.enabled = true;
            _navAgent.Warp(transform.position);
        }
    }

    private void Start()
    {
        FindTargets();
        SetTarget(_spiritTree);
    }

    private void Update()
    {
        EvaluateAggro();

        if (_isRotationEnabled) UpdateRotation(Time.deltaTime);
        if (!_isMovementEnabled) return;

        MoveToTarget();
    }

    public void ApplyStats(EnemyStatsModel stats)
    {
        _moveSpeed = stats.MoveSpeed;
        _rotationSpeed = stats.RotationSpeed;
        _playerStoppingDistance = stats.PlayerStoppingDistance;
        _spiritTreeStoppingDistance = stats.SpiritTreeStoppingDistance;
        _aggroRange = stats.AggroRange;
        _aggroReleaseRange = stats.AggroReleaseRange;

        if (_navAgent != null)
        {
            _navAgent.speed = _moveSpeed;
            _navAgent.angularSpeed = _rotationSpeed;
            _navAgent.updateRotation = true;
            _navAgent.stoppingDistance = CurrentStoppingDistance;
        }
    }

    private void FindTargets()
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
        if (_controller == null) return;

        if (_player == null)
        {
            SetTarget(_spiritTree);
            return;
        }

        float sqrDistanceToPlayer = (_player.position - transform.position).sqrMagnitude;
        float sqrAggroRelease = _aggroReleaseRange * _aggroReleaseRange;
        float sqrAggroRange = _aggroRange * _aggroRange;

        if (_controller.IsAggroedByDamage)
        {
            if (sqrDistanceToPlayer > sqrAggroRelease)
            {
                _controller.ClearDamageAggro();
                SetTarget(_spiritTree);
            }
            else
            {
                SetTarget(_player);
            }
            return;
        }

        if (_controller.HasReachedSpiritTreeOnce)
        {
            SetTarget(_spiritTree);
            return;
        }

        if (_currentTarget == _player)
        {
            if (sqrDistanceToPlayer > sqrAggroRelease) SetTarget(_spiritTree);
            return;
        }

        if (sqrDistanceToPlayer <= sqrAggroRange)
        {
            SetTarget(_player);
            return;
        }

        SetTarget(_spiritTree);
    }

    private void MoveToTarget()
    {
        if (_currentTarget == null)
        {
            SetTarget(_spiritTree);
            return;
        }

        if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

        _navAgent.stoppingDistance = CurrentStoppingDistance;
        _navAgent.SetDestination(_currentTarget.position);
    }

    public void SetTarget(Transform target)
    {
        if (_currentTarget == target) return;

        _currentTarget = target;

        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.stoppingDistance = CurrentStoppingDistance;
        }
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
        else if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = false;
        }
    }

    private void UpdateRotation(float deltaTime)
    {
        if (_isRotationLocked)
        {
            transform.rotation = _lockedRotation;
            return;
        }

        if (_navAgent == null || !_navAgent.enabled) return;

        Vector3 desired = _navAgent.desiredVelocity;
        desired.y = 0f;

        if (desired.sqrMagnitude > 0.0004f)
        {
            Quaternion moveRotation = Quaternion.LookRotation(desired.normalized);
            float maxDegrees = _rotationSpeed * deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, moveRotation, maxDegrees);
            return;
        }

        if (_currentTarget == null) return;

        Vector3 toTarget = _currentTarget.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
        float maxTargetDegrees = _rotationSpeed * deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxTargetDegrees);
    }

    public void LockRotationToTarget()
    {
        if (_currentTarget == null) return;

        Vector3 direction = _currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f) return;

        _lockedRotation = Quaternion.LookRotation(direction.normalized);
        _isRotationLocked = true;
        transform.rotation = _lockedRotation;
    }

    public void UnlockRotation()
    {
        _isRotationLocked = false;
    }

    public void SetRotationEnabled(bool enabled)
    {
        _isRotationEnabled = enabled;
    }
}
