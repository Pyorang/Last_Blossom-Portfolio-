using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitVFXPool : SingletonBehaviour<HitVFXPool>
{
    [SerializeField] private GameObject _normalHitPrefab;
    [SerializeField] private GameObject _enhancedHitPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private Vector3 _hitVFXOffset = new Vector3(0f, 1f, 0f);

    private Queue<ParticleSystem[]> _normalPool = new Queue<ParticleSystem[]>();
    private Queue<ParticleSystem[]> _enhancedPool = new Queue<ParticleSystem[]>();

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreatePoolItem(_normalHitPrefab, _normalPool);
            CreatePoolItem(_enhancedHitPrefab, _enhancedPool);
        }
    }

    private void CreatePoolItem(GameObject prefab, Queue<ParticleSystem[]> pool)
    {
        if (prefab == null) return;
        
        GameObject instance = Instantiate(prefab, _poolParent);
        instance.SetActive(false);
        ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>();
        pool.Enqueue(particles);
    }

    public void Spawn(Vector3 position, bool isEnhanced)
    {
        var pool = isEnhanced ? _enhancedPool : _normalPool;
        var prefab = isEnhanced ? _enhancedHitPrefab : _normalHitPrefab;
        
        if (pool.Count == 0)
        {
            CreatePoolItem(prefab, pool);
        }

        ParticleSystem[] particles = pool.Dequeue();
        Vector3 spawnPosition = position + _hitVFXOffset;

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].transform.position = spawnPosition;
            particles[i].gameObject.SetActive(true);
            particles[i].Play();
        }

        StartCoroutine(ReturnToPool(particles, pool));
    }

    private IEnumerator ReturnToPool(ParticleSystem[] particles, Queue<ParticleSystem[]> pool)
    {
        yield return new WaitForSeconds(1f);
        
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop();
            particles[i].gameObject.SetActive(false);
        }
        pool.Enqueue(particles);
    }
}
