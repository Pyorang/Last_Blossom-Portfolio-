using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SpawnerMovement : MonoBehaviour
{
    private const float MovementSqrMagnitudeThreshold = 0.01f;
    private const float RemainingDistanceTolerance = 0.1f;
    private const float PartialPathDistanceMultiplier = 1.5f;

    private NavMeshAgent _navAgent;
    private Transform _spiritTree;
    private float _stoppingDistance;
    private bool _isMovementEnabled = true;
    private bool _hasEverHadPath;

    public bool IsMoving => _navAgent != null && _navAgent.enabled && _navAgent.velocity.sqrMagnitude > MovementSqrMagnitudeThreshold;

    public bool HasReachedTarget
    {
        get
        {
            if (_spiritTree == null || !_hasEverHadPath || _navAgent == null)
            {
                return false;
            }

            if (_navAgent.pathPending)
            {
                return false;
            }

            if (_navAgent.hasPath && _navAgent.remainingDistance <= _stoppingDistance + RemainingDistanceTolerance)
            {
                return true;
            }

            float distance = Vector3.Distance(transform.position, _spiritTree.position);
            if (_navAgent.pathStatus == NavMeshPathStatus.PathPartial && distance <= _stoppingDistance * PartialPathDistanceMultiplier)
            {
                return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        _hasEverHadPath = false;
        _isMovementEnabled = true;
        if (_navAgent != null)
        {
            _navAgent.enabled = true;
            _navAgent.Warp(transform.position);
        }
    }

    private void Update()
    {
        if (!_isMovementEnabled)
        {
            return;
        }

        if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh)
        {
            return;
        }

        if (_spiritTree != null)
        {
            _navAgent.SetDestination(_spiritTree.position);
            if (_navAgent.hasPath)
            {
                _hasEverHadPath = true;
            }
        }
    }

    public void SetTarget(Transform target)
    {
        _spiritTree = target;
        if (_spiritTree == null || _navAgent == null)
            return;

        if (_navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.SetDestination(_spiritTree.position);
            if (_navAgent.hasPath)
            {
                _hasEverHadPath = true;
            }
        }
    }

    public void Stop()
    {
        if (_navAgent == null) return;

        if (_navAgent.enabled && _navAgent.isOnNavMesh)
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
            return;
        }

        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = false;
        }
    }

    public void ApplyStats(EnemyStatsModel stats)
    {
        _stoppingDistance = stats.SpiritTreeStoppingDistance;
        if (_navAgent == null) return;

        _navAgent.speed = stats.MoveSpeed;
        _navAgent.angularSpeed = stats.RotationSpeed;
        _navAgent.stoppingDistance = _stoppingDistance;
        _navAgent.updateRotation = true;
    }
}
