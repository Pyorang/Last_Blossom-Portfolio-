using UnityEngine;

public class NewMaterialChange : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 _baseScale = Vector3.one;
    [SerializeField] private float _scaleVariation = 0.1f;
    [SerializeField] private float _noiseSpeed = 2f;
    
    private float _noiseOffsetX;
    private float _noiseOffsetY;
    private float _noiseOffsetZ;

    private void Awake()
    {
        _baseScale = transform.localScale;
        
        // 랜덤 오프셋으로 각 오브젝트마다 다른 패턴
        _noiseOffsetX = Random.Range(0f, 100f);
        _noiseOffsetY = Random.Range(0f, 100f);
        _noiseOffsetZ = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float time = Time.time * _noiseSpeed;
        
        float scaleX = _baseScale.x + (Mathf.PerlinNoise(time, _noiseOffsetX) - 0.5f) * 2f * _scaleVariation;
        float scaleY = _baseScale.y + (Mathf.PerlinNoise(time, _noiseOffsetY) - 0.5f) * 2f * _scaleVariation;
        float scaleZ = _baseScale.z + (Mathf.PerlinNoise(time, _noiseOffsetZ) - 0.5f) * 2f * _scaleVariation;
        
        transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }
}
