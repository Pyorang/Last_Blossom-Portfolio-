using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TankAnimator : MonoBehaviour
{
    private const float HitFeedbackScale = 0.97f;
    private const int HitPulseCount = 3;
    private const float HitPulseInterval = 0.03f;

    [SerializeField] private TankMovement _movement;
    [SerializeField] private TankController _controller;

    private Animator _animator;
    private Vector3 _originalScale;
    private bool _isPlayingHitFeedback;
    private int _remainingHitPulses;
    private float _hitPulseTimer;
    private bool _isHitSmall;

    private static readonly int s_isMovingId = Animator.StringToHash("IsMoving");
    private static readonly int s_roarId = Animator.StringToHash("Roar");
    private static readonly int s_groggyId = Animator.StringToHash("Groggy");
    private static readonly int s_dieId = Animator.StringToHash("Die");
    private static readonly int s_isDeadId = Animator.StringToHash("IsDead");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _originalScale = transform.localScale;

        if (_movement == null) _movement = GetComponentInParent<TankMovement>();
        if (_controller == null) _controller = GetComponentInParent<TankController>();
    }

    private void OnEnable()
    {
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);

            _animator.ResetTrigger(s_roarId);
            _animator.ResetTrigger(s_groggyId);
            _animator.ResetTrigger(s_dieId);
            _animator.SetBool(s_isDeadId, false);
        }

        transform.localScale = _originalScale;
        _isPlayingHitFeedback = false;

        if (_controller != null) _controller.OnHitFeedback += PlayHitFeedback;
    }

    private void OnDisable()
    {
        if (_controller != null) _controller.OnHitFeedback -= PlayHitFeedback;
    }

    private void Update()
    {
        if (_movement == null || _controller == null) return;

        bool canMove = _controller.CurrentState == TankState.Chase;
        _animator.SetBool(s_isMovingId, canMove && _movement.IsMoving);

        UpdateHitFeedback();
    }

    private void UpdateHitFeedback()
    {
        if (!_isPlayingHitFeedback) return;

        _hitPulseTimer -= Time.deltaTime;
        if (_hitPulseTimer > 0f) return;

        _hitPulseTimer = HitPulseInterval;
        _isHitSmall = !_isHitSmall;
        transform.localScale = _isHitSmall ? _originalScale * HitFeedbackScale : _originalScale;

        _remainingHitPulses--;
        if (_remainingHitPulses <= 0)
        {
            transform.localScale = _originalScale;
            _isPlayingHitFeedback = false;
        }
    }

    private void PlayHitFeedback()
    {
        if (_controller.CurrentState == TankState.Dead) return;

        _remainingHitPulses = HitPulseCount * 2;
        _hitPulseTimer = 0f;
        _isHitSmall = false;
        _isPlayingHitFeedback = true;
    }

    public void PlayRoar()
    {
        _animator.SetTrigger(s_roarId);
    }

    public void PlayGroggy()
    {
        _animator.SetTrigger(s_groggyId);
    }

    public void PlayDie()
    {
        _animator.SetBool(s_isDeadId, true);
        _animator.SetTrigger(s_dieId);
    }

    public void OnAttackHit()
    {
        _controller?.OnAttackHitFrame();
    }

    public void OnAttackEnd()
    {
        _controller?.OnAttackEndFrame();
    }

    public void OnRoarHit()
    {
        _controller?.OnRoarHitFrame();
    }

    public void OnRoarEnd()
    {
        _controller?.OnRoarEndFrame();
    }

    public void OnDeathAnimationEnd()
    {
        _controller?.OnDeathAnimationComplete();
    }

    public void PlayTrigger(string trigger)
    {
        if (string.IsNullOrEmpty(trigger)) return;
        _animator.SetTrigger(trigger);
    }

    public void ForceReturnToLocomotion()
    {
        if (_animator == null) return;
        _animator.CrossFadeInFixedTime("Idle", 0.08f, 0);
    }
}
