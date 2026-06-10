using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [SerializeField] private AnimationCurve _displacementCurve;
    [SerializeField] private float _displacementMagnitude;
    [SerializeField] private float _lerpSpeed;
    [SerializeField] private float _dissolveSpeed;
    

    private Renderer _renderer;
    private Material _cachedMaterial;
    private bool _isShieldOn;
    private Coroutine _dissolveCoroutine;
    private Coroutine _hitDisplacementCoroutine;
    
    private static readonly int s_hitPosId = Shader.PropertyToID("_HitPos");
    private static readonly int s_displacementStrengthId = Shader.PropertyToID("_DisplacementStrength");
    private static readonly int s_dissolveId = Shader.PropertyToID("_Dissolve");


    private void Start()
    {
        _renderer = GetComponent<Renderer>();
       
        _cachedMaterial = _renderer.material;
    }
    
    private void OnDestroy()
    {
        StopAllEffectCoroutines();
        
        if (_cachedMaterial != null)
        {
            Destroy(_cachedMaterial);
            _cachedMaterial = null;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PlayHitEffect(hit.point);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleShield();
        }
#endif
    }

    public void PlayHitEffect(Vector3 hitPos)
    {
        if (_cachedMaterial == null)
        {
            return;
        }
        
        _cachedMaterial.SetVector(s_hitPosId, hitPos);
        
        if (_hitDisplacementCoroutine != null)
        {
            StopCoroutine(_hitDisplacementCoroutine);
        }
        _hitDisplacementCoroutine = StartCoroutine(HitDisplacementCoroutine());
    }

    public void ToggleShield()
    {
        if (_cachedMaterial == null)
        {
            return;
        }
        
        float target = _isShieldOn ? 0f : 1f;
        _isShieldOn = !_isShieldOn;
        
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
        }
        _dissolveCoroutine = StartCoroutine(DissolveShieldCoroutine(target));
    }
    
    private void StopAllEffectCoroutines()
    {
        if (_hitDisplacementCoroutine != null)
        {
            StopCoroutine(_hitDisplacementCoroutine);
            _hitDisplacementCoroutine = null;
        }
        
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }
    }

    private IEnumerator HitDisplacementCoroutine()
    {
        float lerp = 0f;
        
        while (lerp < 1f)
        {
            float displacement = _displacementCurve.Evaluate(lerp) * _displacementMagnitude;
            _cachedMaterial.SetFloat(s_displacementStrengthId, displacement);
            lerp += Time.deltaTime * _lerpSpeed;
            yield return null;
        }
        
        _hitDisplacementCoroutine = null;
    }

    private IEnumerator DissolveShieldCoroutine(float target)
    {
        float start = _cachedMaterial.GetFloat(s_dissolveId);
        float lerp = 0f;
        
        while (lerp < 1f)
        {
            _cachedMaterial.SetFloat(s_dissolveId, Mathf.Lerp(start, target, lerp));
            lerp += Time.deltaTime * _dissolveSpeed;
            yield return null;
        }
        
        _cachedMaterial.SetFloat(s_dissolveId, target);
        _dissolveCoroutine = null;
    }
}
