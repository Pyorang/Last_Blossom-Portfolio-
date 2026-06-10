using System;
using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    private const float FireCooldownBuffer = 0.5f;
    private const float FacingAngleThreshold = 15f;

    [Header("참조")]
    [SerializeField] private Transform _firePoint;

    private EnemyRangedMovement _movement;
    private EnemyRangedAnimator _animator;
    private Transform _cachedTransform;
    private Transform _player;

    private float _attackDamage;
    private float _attackCooldown;
    private float _lastAttackTime = -999f;
    private float _lastFireTime = -999f;
    private bool _isCharging;
    private bool _isEnabled;
    private bool _hasFiredThisAttack;
    private Transform _currentTarget;

    public bool IsAttacking => _isCharging;

    public event Action OnAttackEnd;

    private void Awake()
    {
        _cachedTransform = transform;
        _movement = GetComponent<EnemyRangedMovement>();
        _animator = GetComponentInChildren<EnemyRangedAnimator>();

        if (_firePoint == null) _firePoint = _cachedTransform;
    }

    private void Start()
    {
        // Movement에서 Player 참조를 가져옴 (MonsterPool에서 주입됨)
        if (_movement != null && _movement.Player != null)
        {
            _player = _movement.Player;
        }
    }

    private void OnEnable()
    {
        _isCharging = false;
        _isEnabled = false;
        _hasFiredThisAttack = false;
        _lastAttackTime = -999f;
        _lastFireTime = -999f;
    }

    private void Update()
    {
        if (!_isEnabled) return;

        _currentTarget = _movement != null ? _movement.CurrentTarget : null;
        if (_currentTarget == null) return;

        if (_isCharging)
        {
            LookAtTarget();
            return;
        }

        TryStartAttack();
    }

    private void TryStartAttack()
    {
        if (_isCharging) return;
        if (Time.time - _lastAttackTime < _attackCooldown) return;
        if (_movement == null || !_movement.HasReachedTarget) return;
        if (!IsFacingTarget()) return;

        StartCharging();
    }

    private bool IsFacingTarget()
    {
        if (_currentTarget == null) return false;

        Vector3 toTarget = (_currentTarget.position - _cachedTransform.position);
        toTarget.y = 0f;
        
        if (toTarget.sqrMagnitude < 0.001f) return true;

        Transform rotateTransform = _movement != null ? _movement.RotationTarget : _cachedTransform;
        float angle = Vector3.Angle(rotateTransform.forward, toTarget.normalized);
        return angle < FacingAngleThreshold;
    }

    private void StartCharging()
    {
        _isCharging = true;
        _hasFiredThisAttack = false;

        LookAtTarget();
        _animator?.PlayTrigger("Attack");
    }

    public void OnFireProjectile()
    {
        if (!_isCharging || _hasFiredThisAttack || _currentTarget == null) return;
        if (Time.time - _lastFireTime < FireCooldownBuffer) return;

        _hasFiredThisAttack = true;
        _lastFireTime = Time.time;

        FireProjectile();
    }

    public void OnAttackAnimationEnd()
    {
        EndAttack();
    }

    private void FireProjectile()
    {
        var pool = EnemyProjectilePool.Instance;
        if (pool == null) return;

        var projectile = pool.GetProjectile(_firePoint.position, _firePoint.rotation);

        if (_currentTarget == _player)
        {
            projectile.LaunchHoming(_player, _attackDamage);
        }

        else
        {
            projectile.LaunchDirect(_currentTarget.position, _attackDamage);
        }

        _lastAttackTime = Time.time;
    }

    private void LookAtTarget()
    {
        if (_currentTarget == null) return;

        Transform rotateTransform = _movement != null ? _movement.RotationTarget : _cachedTransform;
        
        Vector3 direction = _currentTarget.position - _cachedTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            rotateTransform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void EndAttack()
    {
        _isCharging = false;
        _hasFiredThisAttack = false;
        OnAttackEnd?.Invoke();
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled && _isCharging) ForceEndAttack();
    }

    public void ForceEndAttack()
    {
        _isCharging = false;
        _hasFiredThisAttack = false;
    }

    public void ApplyStats(EnemyStatsModel stats)
    {
        _attackDamage = stats.AttackDamage;
        _attackCooldown = stats.AttackCooldown;
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
    }
}
