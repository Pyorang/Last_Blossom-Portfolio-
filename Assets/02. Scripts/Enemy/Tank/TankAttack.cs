using System;
using UnityEngine;

public class TankAttack : MonoBehaviour
{
    private const float KnockbackDistance = 1f;

    [SerializeField] private EnemyHitbox[] _hitboxes;
    [SerializeField] private string[] _comboTriggers;

    private TankMovement _movement;
    private TankAnimator _animator;

    private float _attackDamage;
    private float _attackCooldown;
    private float _lastAttackTime = float.MinValue;
    private float _comboResetTime = 20f;
    private float _lastComboTime = float.MinValue;
    private int _currentComboIndex;
    private bool _isAttacking;
    private bool _isEnabled;
    private int _playerLayerMask;

    public bool IsAttacking => _isAttacking;
    public bool CanAttack => Time.time - _lastAttackTime >= _attackCooldown && !_isAttacking;

    public event Action OnAttackEnd;

    private void Awake()
    {
        _movement = GetComponent<TankMovement>();
        _animator = GetComponentInChildren<TankAnimator>();
        _playerLayerMask = LayerMask.GetMask("Player");
    }

    private void OnEnable()
    {
        _isAttacking = false;
        _isEnabled = false;
        _lastAttackTime = float.MinValue;
        _currentComboIndex = 0;
        _lastComboTime = float.MinValue;
        SetHitboxesActive(false);
    }

    private void Update()
    {
        if (!_isEnabled || _isAttacking) return;
        if (!CanAttack) return;

        StartAttack();
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _lastAttackTime = Time.time;

        _movement?.LockRotationToTarget();

        UpdateComboIndex();
        PlayAttackAnimation();
    }

    private void UpdateComboIndex()
    {
        if (_comboTriggers == null || _comboTriggers.Length == 0) return;

        bool isComboExpired = Time.time - _lastComboTime > _comboResetTime;
        if (isComboExpired) _currentComboIndex = 0;

        _lastComboTime = Time.time;
    }

    private void PlayAttackAnimation()
    {
        if (_animator == null) return;
        if (_comboTriggers == null || _comboTriggers.Length == 0) return;

        string trigger = _comboTriggers[_currentComboIndex];
        _animator.PlayTrigger(trigger);
    }

    private void AdvanceCombo()
    {
        if (_comboTriggers == null || _comboTriggers.Length == 0) return;
        _currentComboIndex = (_currentComboIndex + 1) % _comboTriggers.Length;
    }

    public void ApplyStats(EnemyStatsModel stats)
    {
        _attackDamage = stats.AttackDamage;
        _attackCooldown = stats.AttackCooldown;

        foreach (var hitbox in _hitboxes)
        {
            hitbox?.Initialize(_attackDamage);
        }
    }

    public void OnAttackHitFrame()
    {
        if (!_isAttacking) return;

        SetHitboxesActive(true);

        if (_hitboxes == null || _hitboxes.Length == 0) return;

        foreach (var hitbox in _hitboxes)
        {
            if (hitbox == null) continue;

            var col = hitbox.GetComponent<Collider>();
            if (col == null) continue;

            var overlaps = Physics.OverlapBox(
                col.bounds.center,
                col.bounds.extents,
                col.transform.rotation,
                _playerLayerMask
            );

            foreach (var overlap in overlaps)
            {
                var cc = overlap.GetComponent<CharacterController>();
                if (cc == null) cc = overlap.GetComponentInParent<CharacterController>();
                if (cc == null) continue;

                Vector3 direction = (cc.transform.position - transform.position).normalized;
                direction.y = 0f;

                if (direction == Vector3.zero) direction = transform.forward;

                cc.Move(direction * KnockbackDistance);
            }
        }
    }

    public void OnAttackEndFrame()
    {
        _isAttacking = false;
        SetHitboxesActive(false);

        _movement?.UnlockRotation();

        AdvanceCombo();
        OnAttackEnd?.Invoke();
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled && _isAttacking) ForceEndAttack();
    }

    public void ForceEndAttack()
    {
        _isAttacking = false;
        SetHitboxesActive(false);
        _movement?.UnlockRotation();
    }

    private void SetHitboxesActive(bool active)
    {
        if (_hitboxes == null) return;

        foreach (var hitbox in _hitboxes)
        {
            hitbox?.SetAttacking(active);
        }
    }
}
