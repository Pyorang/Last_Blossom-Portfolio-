using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalVolumeController : SingletonBehaviour<GlobalVolumeController>
{
    [SerializeField] private Volume _globalVolume;

    private const float TARGET_SATURATION = -100f;
    
    private ColorAdjustments _colorAdjustments;
    private Coroutine _currentEffect;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
        
        if (_globalVolume != null && _globalVolume.profile != null)
        {
            _globalVolume.profile.TryGet(out _colorAdjustments);
        }
    }

    public void PlayDesaturationEffect(float duration)
    {
        if (_colorAdjustments == null) return;

        if (_currentEffect != null)
        {
            StopCoroutine(_currentEffect);
        }
        
        _currentEffect = StartCoroutine(DesaturationCoroutine(duration, TARGET_SATURATION));
    }

    private IEnumerator DesaturationCoroutine(float duration, float targetSaturation)
    {
        float half = duration * 0.5f;

        yield return LerpSaturation(0f, targetSaturation, half);

        yield return LerpSaturation(targetSaturation, 0f, half);

        _currentEffect = null;
    }

    private IEnumerator LerpSaturation(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            _colorAdjustments.saturation.value = Mathf.Lerp(from, to, t);
            yield return null;
        }
        _colorAdjustments.saturation.value = to;
    }
}
