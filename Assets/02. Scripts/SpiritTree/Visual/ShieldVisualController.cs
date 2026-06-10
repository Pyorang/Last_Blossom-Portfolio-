using System.Collections;
using UnityEngine;

namespace LastBlossom.SpiritTree.Visual
{
    public class ShieldVisualController : MonoBehaviour
    {
        private const float DefaultFresnelR = 0f;
        private const float DefaultFresnelG = 62f / 255f;
        private const float DefaultFresnelB = 191f / 255f;
        private const float DefaultFresnelIntensity = 3.1f;
        
        private const float DamageFresnelR = 1f;
        private const float DamageFresnelG = 20f / 255f;
        private const float DamageFresnelB = 30f / 255f;
        private const float DamageFresnelIntensity = 2f;
        
        private const float EaseOutPower = 2f;
        private const float EaseInPower = 1.5f;
        
        private const float DissolveMin = 0f;
        private const float DissolveMax = 1f;

        [Header("=== 참조 ===")]
        [Tooltip("보호막 Mesh를 가진 Renderer (자동 할당 가능)")]
        [SerializeField] 
        private Renderer _shieldRenderer;
        
        [Header("=== 대미지 플래시 설정 ===")]
        [Tooltip("대미지 플래시 지속 시간")]
        [SerializeField]
        [Range(0.05f, 0.5f)]
        private float _damageFlashDuration = 0.15f;
        
        [Tooltip("대미지 플래시 복구 시간")]
        [SerializeField]
        [Range(0.1f, 1f)]
        private float _damageFlashRecoveryDuration = 0.3f;
        
        [Header("=== 디졸브 설정 ===")]
        [Tooltip("디졸브 효과 지속 시간")]
        [SerializeField]
        [Range(0.5f, 3f)]
        private float _dissolveDuration = 1.5f;

        private Material _shieldMaterial;
        
        private Coroutine _damageFlashCoroutine;
        private Coroutine _dissolveCoroutine;
        
        private Color _defaultFresnelColor;
        private Color _damageFresnelColor;

        private WaitForSeconds _damageFlashWait;

        private static readonly int s_fresnelColorId = Shader.PropertyToID("Color_cf12b49411d94583a269f83e6981abd1");
        private static readonly int s_dissolveId = Shader.PropertyToID("_Dissolve");
        

        private void Awake()
        {
            InitializeColors();
            InitializeShieldMaterial();

            _damageFlashWait = new WaitForSeconds(_damageFlashDuration);
        }
        
        private void OnDestroy()
        {
            StopAllEffectCoroutines();
            CleanupMaterial();
        }
        
        private void InitializeColors()
        {
            _defaultFresnelColor = new Color(DefaultFresnelR, DefaultFresnelG, DefaultFresnelB) * DefaultFresnelIntensity;
            _damageFresnelColor = new Color(DamageFresnelR, DamageFresnelG, DamageFresnelB) * DamageFresnelIntensity;
        }
        
        private void InitializeShieldMaterial()
        {
            if (_shieldRenderer == null)
            {
                _shieldRenderer = GetComponent<Renderer>();
            }
            
            if (_shieldRenderer == null)
            {
                Debug.LogError($"[ShieldVisualController] {gameObject.name}: Renderer를 찾을 수 없습니다!");
                return;
            }
            
            _shieldMaterial = _shieldRenderer.material;
            

            _shieldMaterial.SetColor(s_fresnelColorId, _defaultFresnelColor);
        }
        
        private void CleanupMaterial()
        {
            if (_shieldMaterial != null)
            {
                Destroy(_shieldMaterial);
                _shieldMaterial = null;
            }
        }
        
        private void StopAllEffectCoroutines()
        {
            if (_damageFlashCoroutine != null)
            {
                StopCoroutine(_damageFlashCoroutine);
                _damageFlashCoroutine = null;
            }
            
            if (_dissolveCoroutine != null)
            {
                StopCoroutine(_dissolveCoroutine);
                _dissolveCoroutine = null;
            }
        }
        
        public void PlayDamageFlash()
        {
            if (_shieldMaterial == null)
            {
                return;
            }
            
            if (_damageFlashCoroutine != null)
            {
                StopCoroutine(_damageFlashCoroutine);
            }
            
            _damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }
        

        public void PlayDissolveEffect()
        {
            if (_shieldMaterial == null)
            {
                return;
            }
            
            if (_dissolveCoroutine != null)
            {
                StopCoroutine(_dissolveCoroutine);
            }
            
            _dissolveCoroutine = StartCoroutine(DissolveCoroutine());
        }
        

        public void RestoreShield()
        {
            StopAllEffectCoroutines();
            
            if (_shieldRenderer != null)
            {
                _shieldRenderer.enabled = true;
            }
            
            if (_shieldMaterial != null)
            {
                _shieldMaterial.SetFloat(s_dissolveId, DissolveMin);
                _shieldMaterial.SetColor(s_fresnelColorId, _defaultFresnelColor);
            }
        }
       
        public void SetShieldActive(bool isActive)
        {
            if (_shieldRenderer != null)
            {
                _shieldRenderer.enabled = isActive;
            }
        }
        
        private IEnumerator DamageFlashCoroutine()
        {
            _shieldMaterial.SetColor(s_fresnelColorId, _damageFresnelColor);
            
            yield return _damageFlashWait;
            
            float elapsedTime = 0f;
            Color startColor = _damageFresnelColor;
            
            while (elapsedTime < _damageFlashRecoveryDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / _damageFlashRecoveryDuration;
                
                float easedProgress = 1f - Mathf.Pow(1f - progress, EaseOutPower);
                
                Color currentColor = Color.Lerp(startColor, _defaultFresnelColor, easedProgress);
                _shieldMaterial.SetColor(s_fresnelColorId, currentColor);
                
                yield return null;
            }
            
            _shieldMaterial.SetColor(s_fresnelColorId, _defaultFresnelColor);
            _damageFlashCoroutine = null;
        }
        
        private IEnumerator DissolveCoroutine()
        {
            float elapsedTime = 0f;
            float startDissolve = _shieldMaterial.GetFloat(s_dissolveId);
            
            while (elapsedTime < _dissolveDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / _dissolveDuration;
                
                float easedProgress = Mathf.Pow(progress, EaseInPower);
                
                float currentDissolve = Mathf.Lerp(startDissolve, DissolveMax, easedProgress);
                _shieldMaterial.SetFloat(s_dissolveId, currentDissolve);
                
                yield return null;
            }
            
            _shieldMaterial.SetFloat(s_dissolveId, DissolveMax);
            
            if (_shieldRenderer != null)
            {
                _shieldRenderer.enabled = false;
            }
            
            _dissolveCoroutine = null;
        }
    }
}
