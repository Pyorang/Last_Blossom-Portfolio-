using System.Collections;
using UnityEngine;

public sealed class TankShield : MonoBehaviour
{
    [SerializeField] private GameObject _shieldEffect;
    [SerializeField] private float _breakPopScale = 1.2f;
    [SerializeField] private float _breakShrinkDuration = 0.25f;

    private bool _isShieldBroken;
    private Vector3 _originalScale;
    private Coroutine _breakRoutine;

    public bool IsShieldBroken => _isShieldBroken;

    private void Awake()
    {
        if (_shieldEffect != null) _originalScale = _shieldEffect.transform.localScale;
    }

    private void OnEnable()
    {
        _isShieldBroken = false;

        if (_breakRoutine != null)
        {
            StopCoroutine(_breakRoutine);
            _breakRoutine = null;
        }

        if (_shieldEffect != null)
        {
            _shieldEffect.transform.localScale = _originalScale;
            _shieldEffect.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (_breakRoutine != null)
        {
            StopCoroutine(_breakRoutine);
            _breakRoutine = null;
        }
    }

    public void BreakShield()
    {
        if (_isShieldBroken) return;

        _isShieldBroken = true;

        if (_shieldEffect != null)
        {
            if (_breakRoutine != null) StopCoroutine(_breakRoutine);
            _breakRoutine = StartCoroutine(BreakSequence());
        }
    }

    private IEnumerator BreakSequence()
    {
        Transform effectTransform = _shieldEffect.transform;
        effectTransform.localScale = _originalScale * _breakPopScale;

        float elapsed = 0f;
        Vector3 startScale = effectTransform.localScale;

        while (elapsed < _breakShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _breakShrinkDuration);
            effectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        effectTransform.localScale = Vector3.zero;
        _shieldEffect.SetActive(false);
        effectTransform.localScale = _originalScale;

        _breakRoutine = null;
    }
}
