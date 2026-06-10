using System;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float _attackDamage = 30f;
    [SerializeField] private float _attackCooldown = 2f;
    
    [Header("콤보 설정")]
    [SerializeField] private string[] _comboTriggers;
    [SerializeField] private float _comboResetTime = 2f;
    
    [Header("참조")]
    [SerializeField] private EnemyMovement _movement;
    [SerializeField] private EnemyAnimator _animator;
    
    [Header("히트박스")]
    [SerializeField] private EnemyHitbox[] _leftHandHitboxes;
    [SerializeField] private EnemyHitbox[] _rightHandHitboxes;
    
    private float _lastAttackTime = -999f;
    private float _lastComboTime = -999f;
    private int _currentComboIndex;
    private bool _isAttacking;
    private bool _isEnabled;
    
    public bool IsAttacking => _isAttacking;
    public bool CanAttack => Time.time - _lastAttackTime >= _attackCooldown && !_isAttacking;
    
    public event Action OnAttackStart;
    public event Action OnAttackEnd;
    
    private void Awake()
    {
        if (_movement == null)
        {
            _movement = GetComponent<EnemyMovement>();
        }
        if (_animator == null)
        {
            _animator = GetComponentInChildren<EnemyAnimator>();
        }
    }

    private void OnEnable()
    {
        _isAttacking = false;
        _isEnabled = false;
        _currentComboIndex = 0;
        _lastAttackTime = -999f;
        _lastComboTime = -999f;
        SetHitboxesAttacking(_leftHandHitboxes, false);
        SetHitboxesAttacking(_rightHandHitboxes, false);
    }
    
    private void Start()
    {
        InitializeHitboxes();
    }
    
    private void Update()
    {
        if (!_isEnabled || _isAttacking)
        {
            return;
        }
        TryAttack();
    }

    private void InitializeHitboxes()
    {
        foreach (var hitbox in _leftHandHitboxes)
        {
            hitbox?.Initialize(_attackDamage);
        }
        foreach (var hitbox in _rightHandHitboxes)
        {
            hitbox?.Initialize(_attackDamage);
        }
    }
    
    private void TryAttack()
    {
        if (!CanAttack)
        {
            return;
        }
        StartAttack();
    }
    
    private void StartAttack()
    {
        _isAttacking = true;
        _lastAttackTime = Time.time;
        
        LookAtTarget();
        UpdateComboIndex();
        
        OnAttackStart?.Invoke();
        PlayAttackAnimation();
    }

    private void UpdateComboIndex()
    {
        if (_comboTriggers == null || _comboTriggers.Length == 0)
        {
            return;
        }

        bool isComboExpired = Time.time - _lastComboTime > _comboResetTime;
        if (isComboExpired)
        {
            _currentComboIndex = 0;
        }
        _lastComboTime = Time.time;
    }

    private void PlayAttackAnimation()
    {
        if (_animator == null)
        {
            return;
        }

        if (_comboTriggers != null && _comboTriggers.Length > 0)
        {
            string trigger = _comboTriggers[_currentComboIndex];
            _animator.PlayTrigger(trigger);
        }
        else
        {
            _animator.PlayTrigger("Attack");
        }
    }

    private void AdvanceCombo()
    {
        if (_comboTriggers == null || _comboTriggers.Length == 0)
        {
            return;
        }
        _currentComboIndex = (_currentComboIndex + 1) % _comboTriggers.Length;
    }
    
    private void LookAtTarget()
    {
        if (_movement == null || _movement.CurrentTarget == null)
        {
            return;
        }
        
        Vector3 direction = (_movement.CurrentTarget.position - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void SetHitboxesAttacking(EnemyHitbox[] hitboxes, bool isAttacking)
    {
        if (hitboxes == null) return;
        
        foreach (var hitbox in hitboxes)
        {
            hitbox?.SetAttacking(isAttacking);
        }
    }
    
    private void EndAttack()
    {
        _isAttacking = false;
        AdvanceCombo();
        OnAttackEnd?.Invoke();
    }
    
    public void OnAttackHitEnd()
    {
        SetHitboxesAttacking(_leftHandHitboxes, false);
        SetHitboxesAttacking(_rightHandHitboxes, false);
    }

    public void OnAttackHitLeft()
    {
        if (!_isAttacking) return;
        SetHitboxesAttacking(_leftHandHitboxes, true);
    }

    public void OnAttackHitRight()
    {
        if (!_isAttacking) return;
        SetHitboxesAttacking(_rightHandHitboxes, true);
    }

    public void OnAttackHitBoth()
    {
        if (!_isAttacking) return;
        SetHitboxesAttacking(_leftHandHitboxes, true);
        SetHitboxesAttacking(_rightHandHitboxes, true);
    }

    public void OnAttackHitFrame()
    {
        OnAttackHitBoth();
    }
    
    public void OnAttackEndFrame()
    {
        EndAttack();
    }
    
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled && _isAttacking)
        {
            ForceEndAttack();
        }
    }
    
    public void ApplyStats(EnemyStatsModel stats)
    {
        _attackDamage = stats.AttackDamage;
        _attackCooldown = stats.AttackCooldown;
        InitializeHitboxes();
    }
    
    public void ForceEndAttack()
    {
        _isAttacking = false;
        SetHitboxesAttacking(_leftHandHitboxes, false);
        SetHitboxesAttacking(_rightHandHitboxes, false);
    }
}
