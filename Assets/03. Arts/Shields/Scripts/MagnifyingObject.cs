using UnityEngine;

public class MagnifyingObject : MonoBehaviour
{
    
    private Renderer _renderer;
    private Material _cachedMaterial;
    private Camera _cam;
    
    private static readonly int s_objScreenPosId = Shader.PropertyToID("_ObjScreenPos");
    
    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        _cam = Camera.main;
        
        _cachedMaterial = _renderer.material;
    }
    
    private void OnDestroy()
    {
        if (_cachedMaterial != null)
        {
            Destroy(_cachedMaterial);
            _cachedMaterial = null;
        }
    }

    private void Update()
    {
        if (_cachedMaterial == null || _cam == null)
        {
            return;
        }
        
        Vector3 screenPoint = _cam.WorldToScreenPoint(transform.position);
        screenPoint.x /= Screen.width;
        screenPoint.y /= Screen.height;
        
        _cachedMaterial.SetVector(s_objScreenPosId, screenPoint);
    }
}
