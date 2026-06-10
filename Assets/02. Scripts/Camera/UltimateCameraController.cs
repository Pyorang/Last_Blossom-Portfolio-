using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class UltimateCameraController : MonoBehaviour
{
    [Header("카메라 참조")]
    [SerializeField] private CinemachineCamera _ultimateCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private CinemachineOrbitalFollow _orbitalFollow;

    [Header("카메라 복귀 각도")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _returnHorizontalOffset = 150f;
    [SerializeField] private float _returnVerticalAngle = -10f;

    [Header("Spline Dolly 설정")]
    [SerializeField] private CinemachineSplineDolly _splineDolly;
    [SerializeField] private float _duration = 2f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("렌더링 설정")]
    [SerializeField] private LayerMask _playerOnlyMask;
    [SerializeField] private Color _backgroundColor = Color.black;

    [Header("UI 참조")]
    [SerializeField] private InGameUIController _uiController;
    [SerializeField] private Camera _overlayCamera;

    private LayerMask _originalCullingMask;
    private CameraClearFlags _originalClearFlags;
    private Color _originalBackgroundColor;
    private CinemachineBlendDefinition _originalBlend;
    private float _elapsed;
    private bool _isPlaying;

    public static event Action OnUltimateCameraComplete;

    public void StartUltimateCamera()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        _elapsed = 0f;

        _originalCullingMask = _mainCamera.cullingMask;
        _originalClearFlags = _mainCamera.clearFlags;
        _originalBackgroundColor = _mainCamera.backgroundColor;

        _mainCamera.cullingMask = _playerOnlyMask;
        _mainCamera.clearFlags = CameraClearFlags.SolidColor;
        _mainCamera.backgroundColor = _backgroundColor;

        _originalBlend = _cinemachineBrain.DefaultBlend;
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        _ultimateCamera.Priority = 30;
        _freeLookCamera.Priority = 10;

        _uiController?.HideGameUI();
        
        if (_overlayCamera != null)
            _overlayCamera.enabled = false;

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
            CompleteUltimateCamera();
        }
    }

    private void CompleteUltimateCamera()
    {
        _isPlaying = false;

        _mainCamera.cullingMask = _originalCullingMask;
        _mainCamera.clearFlags = _originalClearFlags;
        _mainCamera.backgroundColor = _originalBackgroundColor;

        _ultimateCamera.Priority = 0;
        _freeLookCamera.Priority = 20;

        if (_orbitalFollow != null)
        {
            float playerYAngle = _playerTransform != null ? _playerTransform.eulerAngles.y : 0f;
            _orbitalFollow.HorizontalAxis.Value = playerYAngle + _returnHorizontalOffset;
            _orbitalFollow.VerticalAxis.Value = _returnVerticalAngle;
        }

        StartCoroutine(RestoreBlendNextFrame());

        _uiController?.ShowGameUI();
        
        if (_overlayCamera != null)
            _overlayCamera.enabled = true;

        OnUltimateCameraComplete?.Invoke();
    }

    private IEnumerator RestoreBlendNextFrame()
    {
        yield return new WaitForSeconds(0.1f);
        _cinemachineBrain.DefaultBlend = _originalBlend;
    }
}
