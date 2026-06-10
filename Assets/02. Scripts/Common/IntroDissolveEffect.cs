using System.Collections;
using UnityEngine;

public class IntroDissolveEffect : MonoBehaviour
{
    [Header("머티리얼 설정")]
    [SerializeField] private Renderer _targetRenderer;
    [SerializeField] private string _dissolvePropertyName = "_Dissolve";
    
    [Header("애니메이션 설정")]
    [SerializeField] private float _duration = 1.5f;
    [SerializeField] private AnimationCurve _dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== 사운드 설정 ===")]
    [SerializeField] private string _restoreSoundName = "영목_보호막복구";

    private Material _material;
    private int _dissolvePropertyId;
    private Coroutine _dissolveCoroutine;

    private void Awake()
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponent<Renderer>();
        }
        
        if (_targetRenderer != null)
        {
            _material = _targetRenderer.material;
            _dissolvePropertyId = Shader.PropertyToID(_dissolvePropertyName);
        }
    }

    private void OnEnable()
    {
        IntroCameraController.OnIntroComplete += OnIntroComplete;
        
        SetDissolveValue(1f);

        if (!string.IsNullOrEmpty(_restoreSoundName))
        {
            AudioManager.Instance?.PreloadClip(_restoreSoundName);
        }
    }

    private void OnDisable()
    {
        IntroCameraController.OnIntroComplete -= OnIntroComplete;
        StopDissolveAnimation();
    }

    private void OnIntroComplete()
    {
        StartDissolveAnimation();
    }

    private void StartDissolveAnimation()
    {
        StopDissolveAnimation();

        if (!string.IsNullOrEmpty(_restoreSoundName))
        {
            AudioManager.Instance?.PlaySFX(_restoreSoundName, false);
        }
        _dissolveCoroutine = StartCoroutine(DissolveCoroutine());
    }

    private void StopDissolveAnimation()
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }
    }

    private IEnumerator DissolveCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / _duration);
            float curveValue = _dissolveCurve.Evaluate(normalizedTime);
            
            // 1에서 0으로: (1 - curveValue)
            float dissolveValue = 1f - curveValue;
            SetDissolveValue(dissolveValue);

            yield return null;
        }

        SetDissolveValue(0f);
        _dissolveCoroutine = null;
    }

    private void SetDissolveValue(float value)
    {
        if (_material == null) return;
        _material.SetFloat(_dissolvePropertyId, value);
    }
    
    public void PlayDissolveIn()
    {
        StartDissolveAnimation();
    }

    public void SetDissolveImmediate(float value)
    {
        StopDissolveAnimation();
        SetDissolveValue(value);
    }
}
