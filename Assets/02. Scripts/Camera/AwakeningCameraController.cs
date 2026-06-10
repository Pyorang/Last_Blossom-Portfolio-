using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class AwakeningCameraController : MonoBehaviour
{
    [Header("카메라 참조")]
    [SerializeField] private CinemachineCamera _awakeningCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private CinemachineOrbitalFollow _orbitalFollow;

    [Header("카메라 복귀 각도")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _returnHorizontalOffset = 150f;
    [SerializeField] private float _returnVerticalAngle = -10f;

    [Header("Spline Dolly 설정")]
    [SerializeField] private CinemachineSplineDolly _splineDolly;
    [SerializeField] private float _duration = 1.5f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CinemachineBlendDefinition _originalBlend;
    private float _elapsed;
    private bool _isPlaying;

    public static event Action OnAwakeningCameraComplete;

    public void StartAwakeningCamera()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        _elapsed = 0f;

        _originalBlend = _cinemachineBrain.DefaultBlend;
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        _awakeningCamera.Priority = 30;
        _freeLookCamera.Priority = 10;

        if (_splineDolly != null)
        {
            _splineDolly.CameraPosition = 0f;
        }
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        float curveValue = _moveCurve.Evaluate(progress);

        if (_splineDolly != null)
        {
            _splineDolly.CameraPosition = curveValue;
        }

        if (progress >= 1f)
        {
            CompleteAwakeningCamera();
        }
    }

    private void CompleteAwakeningCamera()
    {
        _isPlaying = false;

        _awakeningCamera.Priority = 0;
        _freeLookCamera.Priority = 20;

        if (_orbitalFollow != null)
        {
            float playerYAngle = _playerTransform != null ? _playerTransform.eulerAngles.y : 0f;
            _orbitalFollow.HorizontalAxis.Value = playerYAngle + _returnHorizontalOffset;
            _orbitalFollow.VerticalAxis.Value = _returnVerticalAngle;
        }

        StartCoroutine(RestoreBlendNextFrame());

        OnAwakeningCameraComplete?.Invoke();
    }

    private IEnumerator RestoreBlendNextFrame()
    {
        yield return new WaitForSeconds(0.1f);
        _cinemachineBrain.DefaultBlend = _originalBlend;
    }
}
