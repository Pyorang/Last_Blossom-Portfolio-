using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(MeshTrail))]
[RequireComponent(typeof(HealthComponent))]
public class PlayerMovement : MonoBehaviour
{
    [Header("플레이어 속도")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("회전")]
    [SerializeField, Range(0f, 0.3f)] private float _rotationSmoothTime = 0.12f;

    [Header("애니메이션 블렌드")]
    [SerializeField] private float _speedChangeRate = 10f;

    [Header("회피")]
    [SerializeField] private float _evadeDuration = 0.2f;
    [SerializeField] private float _slowMotionDuration = 0.15f;
    [SerializeField] private float _slowMotionScale = 0.2f;

    [Header("중력")]
    [SerializeField] private float _gravity = -9.81f;

    [Header("공격 이동 장애물 체크")]
    [SerializeField] private float _checkDistance = 0.5f;
    [SerializeField] private LayerMask _obstacleLayer;

    private float _targetRotation;
    private float _rotationVelocity;
    private float _animationBlend;
    private float _verticalVelocity;

    private CharacterController _characterController;
    private PlayerInputHandler _playerInputHandler;
    private PlayerAttack _playerAttack;
    private MeshTrail _meshTrail;
    private HealthComponent _healthComponent;
    private Transform _mainCameraTransform;
    private Animator _animator;

    private WaitForSeconds _evadeDurationWait;
    private WaitForSecondsRealtime _slowMotionDurationWait;

    public float SpeedRatio => _animationBlend;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _playerAttack = GetComponent<PlayerAttack>();   
        _meshTrail = GetComponent<MeshTrail>();
        _healthComponent = GetComponent<HealthComponent>();
        _animator = GetComponent<Animator>();

        if (_mainCameraTransform == null && Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }

        _evadeDurationWait = new WaitForSeconds(_evadeDuration);
        _slowMotionDurationWait = new WaitForSecondsRealtime(_slowMotionDuration);
    }

    private void Update()
    {
        ApplyGravity();
        Move();
    }

    private void ApplyGravity()
    {
        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        Vector3 gravityMove = new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f);
        _characterController.Move(gravityMove);
    }

    private void Move()
    {
        if (_healthComponent.IsInvincible || _playerAttack.IsAttacking) return;

        Vector2 input = _playerInputHandler.MoveInput;

        CalculateAnimationBlend(input.magnitude);

        if (input != Vector2.zero)
        {
            ApplyRotation(input);
            ApplyMovement();
        }
    }

    private void CalculateAnimationBlend(float targetBlend)
    {
        _animationBlend = Mathf.Lerp(_animationBlend, targetBlend, Time.deltaTime * _speedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;
    }

    private void ApplyRotation(Vector2 input)
    {
        _targetRotation = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _mainCameraTransform.eulerAngles.y;

        float rotation = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            _targetRotation,
            ref _rotationVelocity,
            _rotationSmoothTime
        );

        transform.rotation = Quaternion.Euler(0f, rotation, 0f);
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
        _characterController.Move(moveDirection * _moveSpeed * Time.deltaTime);
    }

    public void Evade()
    {
        if (_healthComponent.IsInvincible) return;
        StartCoroutine(EvadeCoroutine());
    }

    public void RotateTowardCamera()
    {
        if (CameraController.Instance != null)
        {
            CameraController.Instance.RotateTransformTowardCamera(transform);
        }
    }

    private IEnumerator EvadeCoroutine()
    {
        _healthComponent.SetInvincibleOn();
        _meshTrail.StartEffect();

        float effectDuration = _evadeDuration + _slowMotionDuration;
        GlobalVolumeController.Instance?.PlayDesaturationEffect(effectDuration);

        try
        {
            yield return _evadeDurationWait;

            _meshTrail.StopEffect();
            TimeScaleManager.Instance?.SetEvadeSlowMotion(_slowMotionScale);

            yield return _slowMotionDurationWait;
        }
        finally
        {
            TimeScaleManager.Instance?.ResetEvadeSlowMotion();
        }
    }

    public void OnEvadeComplete()
    {
        _healthComponent.SetInvincibleOff();
    }

    private void OnAnimatorMove()
    {
        if (!_animator.applyRootMotion) 
            return;
        
        // 궁극기 사용 중이면 애니메이션 Root Motion 그대로 적용
        if (_playerAttack.IsUsingUltimate)
        {
            _characterController.Move(_animator.deltaPosition);
            transform.rotation *= _animator.deltaRotation;
            return;
        }
            
        Vector3 delta = _animator.deltaPosition;
        
        // 공격 중이고 이동량이 있을 때만 장애물 체크
        if (_playerAttack.IsAttacking && delta.magnitude > 0.001f)
        {
            Vector3 origin = transform.position + Vector3.up * (_characterController.height * 0.5f);
            
            bool blocked = Physics.SphereCast(
                origin,
                _characterController.radius,
                delta.normalized,
                out RaycastHit hit,
                _checkDistance,
                _obstacleLayer
            );
            
            if (!blocked)
            {
                _characterController.Move(delta);
            }
        }
        else
        {
            _characterController.Move(delta);
        }
        
        transform.rotation *= _animator.deltaRotation;
    }
}
