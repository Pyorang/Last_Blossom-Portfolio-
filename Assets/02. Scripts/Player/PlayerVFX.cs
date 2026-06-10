using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AwakeningComponent))]
public class PlayerVFX : MonoBehaviour
{
    private const float VFX_Y_ROTATION_OFFSET = 180f;

    [Header("Normal Attack VFX")]
    [SerializeField] private GameObject _normalAttackVFX;
    [SerializeField] private GameObject _justAttackVFX;
    [SerializeField] private Transform _slashLocation;
    [SerializeField] private Transform _swordTransform;

    
    [Header("Ultimate Light")]
    [SerializeField] private Light _ultimateLight;
    [SerializeField] private float _ultimateLightIntensity = 5f;
    [SerializeField] private float _ultimateLightDuration = 0.8f;
    [SerializeField] private AnimationCurve _ultimateLightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f);

    [Header("Ultimate VFX")]
    [SerializeField] private ParticleSystem _ultimateSlashVFX;
    [SerializeField] private Vector3 _ultimateSlashOffset = new Vector3(0f, 1f, 1.5f);
    [SerializeField] private GameObject _ultimateStormVFX;
    [SerializeField] private float _ultimateStormDuration = 1.5f;
    [SerializeField] private Vector3 _ultimateStormOffset = Vector3.zero;
    [SerializeField] private Vector3 _ultimateStormRotation = Vector3.zero;
    [SerializeField] private GameObject _cherryBlossomVFX;
    [SerializeField] private Vector3 _cherryBlossomOffset = new Vector3(0f, 1f, 0f);

    [Header("Awakening Camera")]
    [SerializeField] private AwakeningCameraController _awakeningCameraController;

    [Header("Awakening Head Material")]
    [SerializeField] private Renderer _headRenderer;
    [SerializeField] private Material _awakeningHeadMaterial;

    private AwakeningComponent _awakeningComponent;
    private Material _originalHeadMaterial;

    private ParticleSystem[] _normalSlash;
    private ParticleSystem[] _justSlash;

    

    public Transform SlashLocation => _slashLocation;
    public Transform SwordTransform => _swordTransform;

    private void Awake()
    {
        _awakeningComponent = GetComponent<AwakeningComponent>();

        if (_headRenderer != null)
        {
            _originalHeadMaterial = _headRenderer.material;
        }

        if (_normalAttackVFX != null)
        {
            _normalSlash = _normalAttackVFX.GetComponentsInChildren<ParticleSystem>();
        }
        if (_justAttackVFX != null)
        {
            _justSlash = _justAttackVFX.GetComponentsInChildren<ParticleSystem>();
        }
        
    }

    private void OnEnable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated += OnAwakeningActivated;
            _awakeningComponent.OnAwakeningDeactivated += OnAwakeningDeactivated;
        }
    }

    private void OnDisable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated -= OnAwakeningActivated;
            _awakeningComponent.OnAwakeningDeactivated -= OnAwakeningDeactivated;
        }
    }

    private void OnAwakeningActivated()
    {
        if (_headRenderer != null && _awakeningHeadMaterial != null)
        {
            _headRenderer.material = _awakeningHeadMaterial;
        }
    }

    private void OnAwakeningDeactivated()
    {
        if (_headRenderer != null && _originalHeadMaterial != null)
        {
            _headRenderer.material = _originalHeadMaterial;
        }
    }

    #region Normal Attack VFX

    public void PlayAttackVFX(bool isEnhanced)
    {
        ParticleSystem[] particles = isEnhanced ? _justSlash : _normalSlash;

        if (particles == null || _swordTransform == null || _slashLocation == null) return;

        Quaternion vfxRotation = Quaternion.Euler(0f, transform.eulerAngles.y + VFX_Y_ROTATION_OFFSET, _swordTransform.localEulerAngles.z);

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].transform.SetPositionAndRotation(_slashLocation.position, vfxRotation);
            particles[i].Play();
        }
    }

    public Quaternion GetSkillVFXRotation()
    {
        return Quaternion.Euler(0f, transform.eulerAngles.y + VFX_Y_ROTATION_OFFSET, _swordTransform.localEulerAngles.z);
    }


    #endregion

    #region Hit VFX

    public void PlayHitVFX(Vector3 targetPosition, bool isEnhanced)
    {
        HitVFXPool.Instance?.Spawn(targetPosition, isEnhanced);
    }

    #endregion

    #region Ultimate VFX

    public void TriggerAwakeningCamera()
    {
        if (_awakeningCameraController != null)
        {
            _awakeningCameraController.StartAwakeningCamera();
        }
    }


    public void TriggerCherryBlossomVFX()
    {
        if (_cherryBlossomVFX == null) return;

        Vector3 spawnPosition = transform.position + transform.TransformDirection(_cherryBlossomOffset);
        _cherryBlossomVFX.transform.position = spawnPosition;
        
        ParticleSystem[] particles = _cherryBlossomVFX.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play();
        }
    }

    public void TriggerUltimateLight()
    {
        if (_ultimateLight == null) return;
        StartCoroutine(UltimateLightCoroutine());
    }

    public void TriggerUltimateSlashVFX()
    {
        if (_ultimateSlashVFX == null) return;

        Vector3 spawnPosition = transform.position + transform.TransformDirection(_ultimateSlashOffset);
        Quaternion spawnRotation = Quaternion.Euler(0f, transform.eulerAngles.y + VFX_Y_ROTATION_OFFSET, 0f);

        _ultimateSlashVFX.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        _ultimateSlashVFX.Play(true);
    }

    public void TriggerUltimateStormVFX()
    {
        if (_ultimateStormVFX == null) return;
        StartCoroutine(StormVFXCoroutine());
    }

    #endregion

    #region Coroutines

    private IEnumerator UltimateLightCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < _ultimateLightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _ultimateLightDuration;
            float curveValue = _ultimateLightCurve.Evaluate(t);
            _ultimateLight.intensity = curveValue * _ultimateLightIntensity;
            yield return null;
        }

        _ultimateLight.intensity = 0f;
    }

    private IEnumerator StormVFXCoroutine()
    {
        Vector3 spawnPosition = transform.position + transform.TransformDirection(_ultimateStormOffset);
        Quaternion spawnRotation = Quaternion.Euler(
            _ultimateStormRotation.x, 
            transform.eulerAngles.y + _ultimateStormRotation.y, 
            _ultimateStormRotation.z
        );
        
        _ultimateStormVFX.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        _ultimateStormVFX.SetActive(true);
        yield return new WaitForSeconds(_ultimateStormDuration);
        _ultimateStormVFX.SetActive(false);
    }

    #endregion
}
