using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyRangedMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private Transform _rotationTarget;  // 비어있으면 자기 자신
    
    [Header("공격 설정")]
    [SerializeField] private float _attackRange = 12f;
    [SerializeField] private LayerMask _wallLayer;
    
    [Header("벽 감지 설정")]
    [SerializeField] private float _boxCastWidth = 1f;
    [SerializeField] private float _boxCastHeight = 2f;
    
    private NavMeshAgent _navAgent;
    private Transform _currentTarget;
    private Transform _spiritTree;
    private Transform _player;
    private bool _isMovementEnabled = true;
    private IAggroBehavior _aggroBehavior;
    private Func<bool> _isAttackingProvider;

    public bool IsMoving => _navAgent != null && _navAgent.enabled && _navAgent.velocity.magnitude > 0.1f;
    public Transform CurrentTarget => _currentTarget;
    public float AttackRange => _attackRange;
    public Transform RotationTarget => _rotationTarget != null ? _rotationTarget : transform;
    
    public bool HasReachedTarget => _currentTarget != null && HasClearPathTo(_currentTarget) && IsInAttackRange(_currentTarget);
    
    public bool IsTargetOutOfRange => !HasReachedTarget;
    
    public Transform Player => _player;
    
    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
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
        ConfigureNavAgent();
        FindTargetsIfNeeded();
        _currentTarget = _spiritTree;
    }
    
    public void SetTarget(Transform target)
    {
        _currentTarget = target;
    }
    
    private void Update()
    {
        if (!_isMovementEnabled) return;
        
        EvaluateAggro();
        UpdateNavMeshState();
        MoveOrRotate();
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
        _navAgent.stoppingDistance = _attackRange;
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
        if (_aggroBehavior == null) return;

        var context = new EnemyAggroContext
        {
            Self = transform,
            Player = _player,
            SpiritTree = _spiritTree,
            CurrentTarget = _currentTarget,
            AttackRange = _attackRange,
            HasLineOfSight = HasClearPathTo
        };

        var newTarget = _aggroBehavior.EvaluateTarget(context);
        
        if (newTarget != _currentTarget)
        {
            _currentTarget = newTarget;
        }
    }
    
    private void UpdateNavMeshState()
    {
        if (_currentTarget == null) return;
        
        bool hasClearPath = HasClearPathTo(_currentTarget);
        bool isInRange = IsInAttackRange(_currentTarget);
        bool canAttack = hasClearPath && isInRange;
        
        if (canAttack)
        {
            if (_navAgent.enabled)
            {
                _navAgent.ResetPath();
                _navAgent.enabled = false;
            }
        }
        else
        {
            if (!_navAgent.enabled)
            {
                _navAgent.enabled = true;
                _navAgent.Warp(transform.position);
            }
        }
    }
    
    private void MoveOrRotate()
    {
        if (_currentTarget == null)
        {
            _currentTarget = _spiritTree;
            return;
        }

        bool hasClearPath = HasClearPathTo(_currentTarget);
        bool isInAttackRange = IsInAttackRange(_currentTarget);
        
        if (hasClearPath && isInAttackRange)
        {
            RotateTowardsTarget();
        }
        else
        {
            if (_navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.SetDestination(_spiritTree.position);
            }
        }
    }
    
    private bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        
        return toTarget.sqrMagnitude <= _attackRange * _attackRange;
    }
    
    private void RotateTowardsTarget()
    {
        if (_currentTarget == null) return;
        
        Transform rotateTransform = _rotationTarget != null ? _rotationTarget : transform;
        
        Vector3 direction = _currentTarget.position - transform.position;
        direction.y = 0f;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rotateTransform.rotation = Quaternion.RotateTowards(
                rotateTransform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
    }
    public bool HasClearPathTo(Transform target)
    {
        if (target == null) return false;
        
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 targetPos = target.position + Vector3.up * 1f;
        Vector3 direction = targetPos - origin;
        direction.y = 0f;
        
        float distance = direction.magnitude;
        if (distance < 0.1f) return true;
        
        Vector3 halfExtents = new Vector3(_boxCastWidth * 0.5f, _boxCastHeight * 0.5f, 0.1f);
        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
        
        return !Physics.BoxCast(origin, halfExtents, direction.normalized, rotation, distance, _wallLayer);
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
    
    public void DisableNavAgent()
    {
        if (_navAgent != null) _navAgent.enabled = false;
    }
    
    public void ApplyStats(EnemyStatsModel stats)
    {
        _moveSpeed = stats.MoveSpeed;
        _rotationSpeed = stats.RotationSpeed;
        ConfigureNavAgent();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        DrawCircle(origin, _attackRange, 32);
        
        Transform target = Application.isPlaying ? _currentTarget : null;
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            
            if (distance > 0.1f)
            {
                bool hasClearPath = HasClearPathTo(target);
                Gizmos.color = hasClearPath ? Color.green : Color.red;
                DrawBoxCast(origin, direction.normalized, distance);
            }
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    private void DrawBoxCast(Vector3 origin, Vector3 direction, float distance)
    {
        Vector3 halfExtents = new Vector3(_boxCastWidth * 0.5f, _boxCastHeight * 0.5f, 0.1f);
        Quaternion rotation = Quaternion.LookRotation(direction);
        
        Vector3 endPoint = origin + direction * distance;
        
        Matrix4x4 startMatrix = Matrix4x4.TRS(origin, rotation, Vector3.one);
        Matrix4x4 endMatrix = Matrix4x4.TRS(endPoint, rotation, Vector3.one);
        
        DrawWireBox(startMatrix, halfExtents);
        DrawWireBox(endMatrix, halfExtents);
        
        Vector3 right = rotation * Vector3.right * halfExtents.x;
        Vector3 up = rotation * Vector3.up * halfExtents.y;
        
        Gizmos.DrawLine(origin + right + up, endPoint + right + up);
        Gizmos.DrawLine(origin + right - up, endPoint + right - up);
        Gizmos.DrawLine(origin - right + up, endPoint - right + up);
        Gizmos.DrawLine(origin - right - up, endPoint - right - up);
    }
    
    private void DrawWireBox(Matrix4x4 matrix, Vector3 halfExtents)
    {
        Vector3[] corners = new Vector3[8];
        corners[0] = matrix.MultiplyPoint3x4(new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z));
        corners[1] = matrix.MultiplyPoint3x4(new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z));
        corners[2] = matrix.MultiplyPoint3x4(new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z));
        corners[3] = matrix.MultiplyPoint3x4(new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z));
        corners[4] = matrix.MultiplyPoint3x4(new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z));
        corners[5] = matrix.MultiplyPoint3x4(new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z));
        corners[6] = matrix.MultiplyPoint3x4(new Vector3(halfExtents.x, halfExtents.y, halfExtents.z));
        corners[7] = matrix.MultiplyPoint3x4(new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z));
        
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
        
        Gizmos.DrawLine(corners[4], corners[5]);
        Gizmos.DrawLine(corners[5], corners[6]);
        Gizmos.DrawLine(corners[6], corners[7]);
        Gizmos.DrawLine(corners[7], corners[4]);
        
        Gizmos.DrawLine(corners[0], corners[4]);
        Gizmos.DrawLine(corners[1], corners[5]);
        Gizmos.DrawLine(corners[2], corners[6]);
        Gizmos.DrawLine(corners[3], corners[7]);
    }
#endif
}
