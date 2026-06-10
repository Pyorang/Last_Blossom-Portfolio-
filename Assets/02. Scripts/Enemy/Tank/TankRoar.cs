using System;
using UnityEngine;

public class TankRoar : MonoBehaviour
{
    private const float RoarCooldown = 5f;
    private const float KnockbackDistance = 3f;

    private TankMovement _movement;
    private TankAnimator _animator;
    private Collider[] _allColliders;

    private float _roarDamage;
    private float _roarTimer;
    private bool _isRoaring;
    private bool _isPaused;
    private bool _isCooldownActive;

    public bool IsRoaring => _isRoaring;

    public event Action OnRoarEnd;

    private void Awake()
    {
        _movement = GetComponent<TankMovement>();
        _animator = GetComponentInChildren<TankAnimator>();
    }

    private void Start()
    {
        _allColliders = GetComponentsInChildren<Collider>(true);
        _roarTimer = RoarCooldown;
        _isCooldownActive = false;
    }

    private void OnEnable()
    {
        _roarTimer = RoarCooldown;
        _isRoaring = false;
        _isPaused = false;
        _isCooldownActive = false;
    }

    public void SetDamage(float damage)
    {
        _roarDamage = damage;
    }

    public void StartCooldownFromFirstPlayerAttack()
    {
        if (_isCooldownActive) return;

        _isCooldownActive = true;
        _roarTimer = RoarCooldown;
    }

    public void UpdateTimer(float deltaTime)
    {
        if (!_isCooldownActive) return;
        if (_isPaused || _isRoaring) return;

        _roarTimer -= deltaTime;
    }

    public bool IsReady()
    {
        return _isCooldownActive && _roarTimer <= 0f && !_isRoaring;
    }

    public void StartRoar()
    {
        if (_isRoaring) return;

        _isRoaring = true;
        _movement?.LockRotationToTarget();
        _animator?.PlayRoar();
    }

    public void OnRoarHitFrame()
    {
        if (!_isRoaring) return;

        Transform player = _movement?.Player;
        if (player == null) return;

        if (!IsPlayerTouchingAnyCollider(player)) return;

        if (player.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(_roarDamage);
        }

        if (player.TryGetComponent<CharacterController>(out CharacterController cc))
        {
            Vector3 direction = (cc.transform.position - transform.position).normalized;
            direction.y = 0f;

            if (direction == Vector3.zero) direction = transform.forward;

            cc.Move(direction * KnockbackDistance);
        }
    }

    private bool IsPlayerTouchingAnyCollider(Transform player)
    {
        if (_allColliders == null || _allColliders.Length == 0) return false;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null) return false;

        for (int i = 0; i < _allColliders.Length; i++)
        {
            Collider col = _allColliders[i];
            if (col == null || !col.enabled) continue;

            if (col.bounds.Intersects(playerCollider.bounds)) return true;
        }

        return false;
    }

    public void OnRoarEndFrame()
    {
        if (!_isRoaring) return;

        _isRoaring = false;
        _movement?.UnlockRotation();
        _isCooldownActive = true;
        _roarTimer = RoarCooldown;

        OnRoarEnd?.Invoke();
    }

    public void ForceEndRoar()
    {
        if (!_isRoaring) return;

        _isRoaring = false;
        _movement?.UnlockRotation();
        _isCooldownActive = true;
        _roarTimer = RoarCooldown;
    }

    public void PauseTimer()
    {
        _isPaused = true;
    }

    public void ResumeTimer()
    {
        _isPaused = false;
    }
}
