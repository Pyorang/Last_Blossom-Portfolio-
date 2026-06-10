using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraController : SingletonBehaviour<CameraController>
{
    [Header("Shake 설정")]
    [SerializeField] private float _defaultForce = 0.3f;
    [SerializeField] private float _comboResetTime = 0.3f;
    [SerializeField] private float _minForceMultiplier = 0.2f;
    [SerializeField] private Vector3 _strongForce = new Vector3(0.5f, 1.5f, 0f);

    private CinemachineImpulseSource _impulseSource;
    private Transform _cameraTransform;
    private int _comboCount;
    private float _lastHitTime;
    private int _lastTargetId;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();

        _impulseSource = GetComponent<CinemachineImpulseSource>();

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    public void SetCamera(Camera camera)
    {
        if (camera != null)
        {
            _cameraTransform = camera.transform;
        }
    }

    public void Shake(float baseForce = -1f, int targetId = -1)
    {
        if (_impulseSource == null) return;

        if (targetId >= 0)
        {
            if (targetId != _lastTargetId)
                _comboCount = 0;
            _lastTargetId = targetId;
        }
        else if (Time.time - _lastHitTime > _comboResetTime)
        {
            _comboCount = 0;
        }

        _comboCount++;
        _lastHitTime = Time.time;

        float force = baseForce < 0 ? _defaultForce : baseForce;
        float multiplier = Mathf.Max(_minForceMultiplier, 1f / _comboCount);
        _impulseSource.GenerateImpulse(force * multiplier);
    }

    public void StrongShake()
    {
        if (_impulseSource == null) return;
        _impulseSource.GenerateImpulse(_strongForce);
    }

    public void RotateTransformTowardCamera(Transform target)
    {
        if (_cameraTransform == null || target == null) return;

        Vector3 cameraForward = _cameraTransform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            target.rotation = Quaternion.LookRotation(cameraForward.normalized);
        }
    }

    public Vector3 GetCameraForward()
    {
        if (_cameraTransform == null) return Vector3.forward;

        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    public Quaternion GetCameraRotationFlat()
    {
        if (_cameraTransform == null) return Quaternion.identity;

        Vector3 forward = GetCameraForward();
        if (forward.sqrMagnitude < 0.001f) return Quaternion.identity;

        return Quaternion.LookRotation(forward);
    }
}
