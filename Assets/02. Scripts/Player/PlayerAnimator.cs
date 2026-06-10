using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(AwakeningComponent))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private PlayerMovement _movement;
    private HealthComponent _healthComponent;
    private AwakeningComponent _awakeningComponent;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int EvadeHash = Animator.StringToHash("Evade");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SkillHash = Animator.StringToHash("Skill");
    private static readonly int Hurt1Hash = Animator.StringToHash("Hurt1");
    private static readonly int Hurt2Hash = Animator.StringToHash("Hurt2");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");
    private static readonly int AwakeningHash = Animator.StringToHash("Awakening");
    private static readonly int AwakenedHash = Animator.StringToHash("Awakened");
    private static readonly int HasTwinWindHash = Animator.StringToHash("HasTwinWind");

    private bool _useHurt1 = true;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
        _healthComponent = GetComponent<HealthComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
    }

    private void OnEnable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated += OnAwakeningActivated;
            _awakeningComponent.OnAwakeningDeactivated += OnAwakeningDeactivated;
        }
    }

    private void OnDisable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated -= OnAwakeningActivated;
            _awakeningComponent.OnAwakeningDeactivated -= OnAwakeningDeactivated;
        }
    }

    private void Start()
    {
        _healthComponent.OnDamageTaken += OnDamageTaken;
        _healthComponent.OnDeath += OnDeath;
    }

    private void OnDestroy()
    {
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageTaken -= OnDamageTaken;
            _healthComponent.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        _animator.SetFloat(SpeedHash, _movement.SpeedRatio);
    }

    private void OnDamageTaken(float damage)
    {
        PlayHurtAnimation();
    }

    private void OnDeath()
    {
        PlayDieAnimation();
    }

    private void OnAwakeningActivated()
    {
        _animator.SetBool(AwakenedHash, true);
    }

    private void OnAwakeningDeactivated()
    {
        _animator.SetBool(AwakenedHash, false);
    }

    #region Animation Triggers

    public void PlayEvadeAnimation()
    {
        _movement.RotateTowardCamera();
        _animator.SetTrigger(EvadeHash);
    }

    public void PlayAttackAnimation()
    {
        _movement.RotateTowardCamera();
        _animator.SetTrigger(AttackHash);
    }

    public void PlaySkillAnimation()
    {
        _movement.RotateTowardCamera();
        _animator.SetTrigger(SkillHash);
    }

    public void PlayHurtAnimation()
    {
        _animator.SetTrigger(_useHurt1 ? Hurt1Hash : Hurt2Hash);
        _useHurt1 = !_useHurt1;
    }

    public void PlayDieAnimation()
    {
        _animator.SetTrigger(DieHash);
    }

    public void PlayUltimateAnimation()
    {
        _movement.RotateTowardCamera();
        _animator.SetTrigger(UltimateHash);
    }

    public void PlayAwakeningAnimation()
    {
        _animator.SetTrigger(AwakeningHash);
    }

    public void SetTwinWind(bool value)
    {
        _animator.SetBool(HasTwinWindHash, value);
    }

    #endregion
}
