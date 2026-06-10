using UnityEngine;
using System.Collections;

public class TimeScaleManager : SingletonBehaviour<TimeScaleManager>
{
    [Header("Hit Stop Settings")]
    [SerializeField] private float _lightDuration = 0.02f;
    [SerializeField] private float _lightScale = 0.1f;
    [SerializeField] private float _mediumDuration = 0.05f;
    [SerializeField] private float _mediumScale = 0.1f;

    private float _baseTimeScale = 1f;
    private float _hitStopMultiplier = 1f;
    private float _evadeMultiplier = 1f;
    private float _pauseMultiplier = 1f;

    private Coroutine _hitStopCoroutine;
    private float _originalFixedDeltaTime;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
        _originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void UpdateTimeScale()
    {
        Time.timeScale = _baseTimeScale * _hitStopMultiplier * _evadeMultiplier * _pauseMultiplier;
        Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;
    }

    #region Hit Stop

    public void PlayLightHitStop()
    {
        PlayHitStop(_lightDuration, _lightScale);
    }

    public void PlayMediumHitStop()
    {
        PlayHitStop(_mediumDuration, _mediumScale);
    }

    public void PlayHitStop(float duration, float scale)
    {
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
        }

        _hitStopCoroutine = StartCoroutine(HitStopCoroutine(duration, scale));
    }

    private IEnumerator HitStopCoroutine(float duration, float scale)
    {
        _hitStopMultiplier = scale;
        UpdateTimeScale();

        yield return new WaitForSecondsRealtime(duration);

        _hitStopMultiplier = 1f;
        UpdateTimeScale();
        _hitStopCoroutine = null;
    }

    #endregion

    #region Evade

    public void SetEvadeSlowMotion(float scale)
    {
        _evadeMultiplier = scale;
        UpdateTimeScale();
    }

    public void ResetEvadeSlowMotion()
    {
        _evadeMultiplier = 1f;
        UpdateTimeScale();
    }

    #endregion

    #region Pause

    public void Pause()
    {
        _pauseMultiplier = 0f;
        UpdateTimeScale();
    }

    public void Resume()
    {
        _pauseMultiplier = 1f;
        UpdateTimeScale();
    }

    #endregion

    #region Base

    public void SetBaseTimeScale(float scale)
    {
        _baseTimeScale = scale;
        UpdateTimeScale();
    }

    #endregion
}
