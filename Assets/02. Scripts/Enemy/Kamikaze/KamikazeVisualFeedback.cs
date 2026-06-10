using System.Collections;
using UnityEngine;

public class KamikazeVisualFeedback : MonoBehaviour
{
    [Header("점멸 설정")]
    [SerializeField] private Renderer _glowRenderer;
    [SerializeField] private Color _baseGlowColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color _peakGlowColor = new Color(1f, 0f, 0f);
    [SerializeField] private float _baseBlinkInterval = 0.3f;
    [SerializeField] private float _minBlinkInterval = 0.05f;

    [Header("폭발 이펙트")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private float _effectDuration = 2f;

    private Material _glowMaterial;
    private Coroutine _blinkCoroutine;
    private bool _isGlowOn;

    private static readonly int s_emissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (_glowRenderer != null)
        {
            _glowMaterial = _glowRenderer.material;
        }
    }

    private void OnEnable()
    {
        SetGlowColor(_baseGlowColor);
        _isGlowOn = false;
    }

    private void OnDisable()
    {
        StopAllFeedback();
    }

    private void OnDestroy()
    {
        if (_glowMaterial != null)
        {
            Destroy(_glowMaterial);
        }
    }

    public void StartChaseFeedback()
    {
        StopAllFeedback();
        _blinkCoroutine = StartCoroutine(BlinkCoroutine(_baseBlinkInterval));
    }

    public void StartDetonationFeedback(float duration)
    {
        StopAllFeedback();
        _blinkCoroutine = StartCoroutine(DetonationBlinkCoroutine(duration));
    }

    public void StopAllFeedback()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        SetGlowColor(_baseGlowColor);
    }

    // NOTE: 폭발 이펙트 최적화 필요
    public void PlayExplosionEffect()
    {
        if (_explosionEffectPrefab != null)
        {
            var effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }
    }

    private IEnumerator BlinkCoroutine(float interval)
    {
        var wait = new WaitForSeconds(interval);

        while (true)
        {
            ToggleGlow(1f);
            yield return wait;
        }
    }

    private IEnumerator DetonationBlinkCoroutine(float totalDuration)
    {
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            float progress = elapsed / totalDuration;
            float interval = Mathf.Lerp(_baseBlinkInterval, _minBlinkInterval, progress);
            float intensity = Mathf.Lerp(1f, 3f, progress);

            ToggleGlow(intensity);
            yield return new WaitForSeconds(interval * 0.5f);

            elapsed += interval * 0.5f;
        }

        SetGlowColor(_peakGlowColor * 5f);
    }

    private void ToggleGlow(float intensityMultiplier)
    {
        _isGlowOn = !_isGlowOn;
        SetGlowColor(_isGlowOn ? _peakGlowColor * intensityMultiplier : _baseGlowColor);
    }

    private void SetGlowColor(Color color)
    {
        if (_glowMaterial != null)
        {
            _glowMaterial.SetColor(s_emissionColorId, color);
        }
    }
}
