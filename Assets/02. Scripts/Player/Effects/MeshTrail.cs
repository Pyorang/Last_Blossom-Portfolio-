using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTrail : MonoBehaviour
{
    [Header("Mesh Related")]
    [SerializeField] private float _meshRefreshRate = 0.1f;
    [SerializeField] private float _meshDestroyDelay = 0.5f;
    [SerializeField] private int _poolSizePerRenderer = 10;
    [SerializeField] private Transform _trailPoolParent;

    [Header("Shader Related")]
    [SerializeField] private Material _trailMaterial;
    [SerializeField] private string _shaderVarRef;
    [SerializeField] private float _shaderVarRate = 0.1f;
    [SerializeField] private float _shaderVarRefreshRate = 0.05f;

    private bool _isTrailActive;
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    private WaitForSeconds _refreshWait;
    private WaitForSeconds _shaderRefreshWait;
    private int _shaderVarId;
    
    private Queue<TrailMeshData> _pool;
    private List<TrailMeshData> _activeTrails;

    private class TrailMeshData
    {
        public GameObject GameObject;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public Mesh Mesh;
        public MaterialPropertyBlock PropertyBlock;
        public float DeactivateTime;
    }

    private void Awake()
    {
        _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _refreshWait = new WaitForSeconds(_meshRefreshRate);
        _shaderRefreshWait = new WaitForSeconds(_shaderVarRefreshRate);
        _shaderVarId = Shader.PropertyToID(_shaderVarRef);
        
        int totalPoolSize = _skinnedMeshRenderers.Length * _poolSizePerRenderer;
        _pool = new Queue<TrailMeshData>(totalPoolSize);
        _activeTrails = new List<TrailMeshData>(totalPoolSize);
        
        InitializePool(totalPoolSize);
    }

    private void InitializePool(int size)
    {
        for (int i = 0; i < size; i++)
        {
            var data = CreateTrailMeshData();
            data.MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            data.GameObject.SetActive(false);
            _pool.Enqueue(data);
        }
    }

    private TrailMeshData CreateTrailMeshData()
    {
        var go = new GameObject("TrailMesh");
        
        if (_trailPoolParent != null)
        {
            go.transform.SetParent(_trailPoolParent, false);
        }
        
        var data = new TrailMeshData
        {
            GameObject = go,
            MeshFilter = go.AddComponent<MeshFilter>(),
            MeshRenderer = go.AddComponent<MeshRenderer>(),
            Mesh = new Mesh(),
            PropertyBlock = new MaterialPropertyBlock()
        };
        
        data.MeshFilter.mesh = data.Mesh;
        data.MeshRenderer.sharedMaterial = _trailMaterial;
        
        return data;
    }

    private TrailMeshData GetFromPool()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }
        
        return CreateTrailMeshData();
    }

    private void ReturnToPool(TrailMeshData data)
    {
        data.GameObject.SetActive(false);
        _pool.Enqueue(data);
    }

    private void Update()
    {
        for (int i = _activeTrails.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _activeTrails[i].DeactivateTime)
            {
                ReturnToPool(_activeTrails[i]);
                _activeTrails.RemoveAt(i);
            }
        }
    }

    public void StartEffect()
    {
        if (!_isTrailActive)
        {
            _isTrailActive = true;
            StartCoroutine(ActivateTrailCoroutine());
        }
    }

    public void StopEffect()
    {
        _isTrailActive = false;
    }

    private IEnumerator ActivateTrailCoroutine()
    {
        while (_isTrailActive)
        {
            SpawnTrailMeshes();
            yield return _refreshWait;
        }
    }

    private IEnumerator AnimateMaterialFloat(TrailMeshData trailData, float goal, float rate)
    {
        float value = 1f;

        while (value > goal)
        {
            value -= rate;
            trailData.PropertyBlock.SetFloat(_shaderVarId, value);
            trailData.MeshRenderer.SetPropertyBlock(trailData.PropertyBlock);
            yield return _shaderRefreshWait;
        }
    }

    private void SpawnTrailMeshes()
    {
        foreach (var skinnedRenderer in _skinnedMeshRenderers)
        {
            var trailData = GetFromPool();
            var trailTransform = trailData.GameObject.transform;
            
            trailTransform.SetPositionAndRotation(
                skinnedRenderer.transform.position,
                skinnedRenderer.transform.rotation
            );
            trailTransform.localScale = skinnedRenderer.transform.lossyScale;
            
            skinnedRenderer.BakeMesh(trailData.Mesh);
            
            trailData.PropertyBlock.SetFloat(_shaderVarId, 1f);
            trailData.MeshRenderer.SetPropertyBlock(trailData.PropertyBlock);

            trailData.GameObject.SetActive(true);
            StartCoroutine(AnimateMaterialFloat(trailData, 0f, _shaderVarRate));

            trailData.DeactivateTime = Time.time + _meshDestroyDelay;
            
            _activeTrails.Add(trailData);
        }
    }

    private void OnDestroy()
    {
        while (_pool.Count > 0)
        {
            var data = _pool.Dequeue();
            if (data.Mesh != null) Destroy(data.Mesh);
        }
        
        foreach (var data in _activeTrails)
        {
            if (data.Mesh != null) Destroy(data.Mesh);
        }
    }
}
