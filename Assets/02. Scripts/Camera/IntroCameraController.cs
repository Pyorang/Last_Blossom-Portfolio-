using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class IntroCameraController : MonoBehaviour
{
    [Header("카메라 참조")]
    [SerializeField] private CinemachineCamera _introCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineBrain _cinemachineBrain;

    [Header("오버레이 카메라 (블렌딩 후 활성화)")]
    [SerializeField] private Camera[] _overlayCameras;

    [Header("FreeLook 입력 컨트롤러")]
    [SerializeField] private CinemachineInputAxisController _freeLookInput;

    [Header("Spline Dolly 설정")]
    [SerializeField] private CinemachineSplineDolly _splineDolly;
    [SerializeField] private float _duration = 1.7f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float _elapsed;
    private bool _isPlaying;

    public static event System.Action OnIntroComplete;

    private void Start()
    {
        StartIntro();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        float curveValue = _moveCurve.Evaluate(progress);

        if (_splineDolly != null)
        {
            _splineDolly.CameraPosition = curveValue;
        }

        if (progress >= 1f)
        {
            CompleteIntro();
        }
    }

    private void StartIntro()
    {
        _isPlaying = true;
        _elapsed = 0f;

        SetOverlayCamerasEnabled(false);

        if (_freeLookInput != null)
        {
            _freeLookInput.enabled = false;
        }

        if (_introCamera != null)
        {
            _introCamera.Priority = 20;
        }

        if (_freeLookCamera != null)
        {
            _freeLookCamera.Priority = 10;
        }

        if (_splineDolly != null)
        {
            _splineDolly.CameraPosition = 0f;
        }
    }

    private void CompleteIntro()
    {
        _isPlaying = false;

        if (_introCamera != null)
        {
            _introCamera.Priority = 0;
        }

        if (_freeLookCamera != null)
        {
            _freeLookCamera.Priority = 20;
        }

        if (_freeLookInput != null)
        {
            _freeLookInput.enabled = true;
        }

        StartCoroutine(CompleteAfterBlend());
    }

    private IEnumerator CompleteAfterBlend()
    {
        float blendDuration = _cinemachineBrain != null 
            ? _cinemachineBrain.DefaultBlend.Time 
            : 1f;

        yield return new WaitForSeconds(blendDuration);

        SetOverlayCamerasEnabled(true);
        OnIntroComplete?.Invoke();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGame();
        }
    }

    private void SetOverlayCamerasEnabled(bool enabled)
    {
        for (int i = 0; i < _overlayCameras.Length; i++)
        {
            if (_overlayCameras[i] != null)
            {
                _overlayCameras[i].enabled = enabled;
            }
        }
    }

    public void SkipIntro()
    {
        if (!_isPlaying) return;

        if (_splineDolly != null)
        {
            _splineDolly.CameraPosition = 1f;
        }

        CompleteIntro();
    }
}
