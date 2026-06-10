using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class KamikazeMovement : MonoBehaviour
{
    private NavMeshAgent _navAgent;
    private Transform _spiritTree;

    private float _stoppingDistance;
    private bool _isMovementEnabled = true;
    private bool _hasEverHadPath;

    public Transform CurrentTarget => _spiritTree;
    public bool IsMoving => _navAgent.enabled && _navAgent.velocity.sqrMagnitude > 0.01f;

    public bool HasReachedTarget
    {
        get
        {
            if (_spiritTree == null || !_hasEverHadPath)
            {
                return false;
            }

            if (!_navAgent.pathPending)
            {
                if (_navAgent.hasPath && _navAgent.remainingDistance <= _stoppingDistance + 0.1f)
                {
                    return true;
                }

                float distance = Vector3.Distance(transform.position, _spiritTree.position);
                if (_navAgent.pathStatus == NavMeshPathStatus.PathPartial && distance <= _stoppingDistance * 1.5f)
                {
                    return true;
                }
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
        _navAgent.enabled = true;
        _navAgent.Warp(transform.position);
    }

    private void Start()
    {
        FindSpiritTree();
    }

    private void Update()
    {
        if (!_isMovementEnabled)
        {
            return;
        }

        if (_spiritTree == null)
        {
            FindSpiritTree();
        }

        if (_navAgent == null)
        {
            return;
        }

        if (!_hasEverHadPath && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.SetDestination(_spiritTree.position);
            if (_navAgent.hasPath)
            {
                _hasEverHadPath = true;
            }
        }
    }

    private void FindSpiritTree()
    {
        // MonsterPool에서 SetTargets()로 주입받음
        // Find 제거됨
    }

    public void SetTargets(Transform spiritTree)
    {
        _spiritTree = spiritTree;
    }

    public void Stop()
    {
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
        }
        else if (_navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = false;
        }
    }

    public void ApplyStats(EnemyStatsModel stats)
    {
        _stoppingDistance = stats.SpiritTreeStoppingDistance;

        _navAgent.speed = stats.MoveSpeed;
        _navAgent.angularSpeed = stats.RotationSpeed;
        _navAgent.stoppingDistance = _stoppingDistance;
        _navAgent.updateRotation = true;
    }
}
