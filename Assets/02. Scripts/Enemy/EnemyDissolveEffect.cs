using AmazingAssets.AdvancedDissolve;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDissolveEffect : MonoBehaviour
{
    [Header("데이터 설정")]
    [SerializeField] private float _dissolveDuration = 1.5f;
    [SerializeField] private float _dissolveDelay = 0.5f;
    [SerializeField] private AnimationCurve _dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("컴포넌트 참조")]
    [SerializeField] private Renderer[] _renderers;

    private Material[] _materials;
    private Coroutine _dissolveCoroutine;
    private bool _isDissolving;

    private IDeathAnimationEndNotifier[] _deathNotifiers;

    public event Action OnDissolveComplete;

    private void Awake()
    {
        _deathNotifiers = GetComponents<IDeathAnimationEndNotifier>();

        CacheRenderers();
        CacheMaterials();
    }

    private void OnEnable()
    {
        ResetDissolve();

        if (_deathNotifiers == null) return;

        for (int i = 0; i < _deathNotifiers.Length; i++)
        {
            if (_deathNotifiers[i] == null) continue;
            _deathNotifiers[i].OnDeathAnimationEnd -= OnDeathAnimationEnded;
            _deathNotifiers[i].OnDeathAnimationEnd += OnDeathAnimationEnded;
        }
    }

    private void OnDisable()
    {
        if (_deathNotifiers != null)
        {
            for (int i = 0; i < _deathNotifiers.Length; i++)
            {
                if (_deathNotifiers[i] == null) continue;
                _deathNotifiers[i].OnDeathAnimationEnd -= OnDeathAnimationEnded;
            }
        }

        StopDissolve();
    }

    private void CacheRenderers()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void CacheMaterials()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _materials = Array.Empty<Material>();
            return;
        }

        var materialList = new List<Material>();

        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            materialList.AddRange(renderer.materials);
        }

        _materials = materialList.ToArray();
    }

    private void OnDeathAnimationEnded()
    {
        StartDissolve();
    }

    private void StartDissolve()
    {
        if (_isDissolving) return;
        _dissolveCoroutine = StartCoroutine(DissolveCoroutine());
    }

    private void StopDissolve()
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }

        _isDissolving = false;
    }

    private IEnumerator DissolveCoroutine()
    {
        _isDissolving = true;

        if (_dissolveDelay > 0f)
        {
            yield return new WaitForSeconds(_dissolveDelay);
        }

        float elapsedTime = 0f;

        while (elapsedTime < _dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / _dissolveDuration);
            float clipValue = _dissolveCurve.Evaluate(normalizedTime);

            ApplyDissolveValue(clipValue);

            yield return null;
        }

        ApplyDissolveValue(1f);
        CompleteDissolve();
    }

    private void ApplyDissolveValue(float clipValue)
    {
        if (_materials == null) return;

        foreach (var material in _materials)
        {
            if (material == null) continue;

            AdvancedDissolveProperties.Cutout.Standard.UpdateLocalProperty(
                material,
                AdvancedDissolveProperties.Cutout.Standard.Property.Clip,
                clipValue
            );
        }
    }

    private void ResetDissolve()
    {
        StopDissolve();
        ApplyDissolveValue(0f);
    }

    private void CompleteDissolve()
    {
        _isDissolving = false;
        OnDissolveComplete?.Invoke();

        WaveManager.Instance?.OnMonsterDeath(gameObject);
        MonsterPool.Instance?.Despawn(gameObject);
    }
}
