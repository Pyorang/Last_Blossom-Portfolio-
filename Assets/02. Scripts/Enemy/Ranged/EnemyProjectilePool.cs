using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectilePool : MonoBehaviour
{
    private const int DefaultPoolSize = 50;

    [Header("풀 설정")]
    [SerializeField] private EnemyProjectile _projectilePrefab;
    [SerializeField] private int _initialPoolSize = DefaultPoolSize;

    private Queue<EnemyProjectile> _available;
    private List<EnemyProjectile> _all;
    private Transform _container;

    private static EnemyProjectilePool s_instance;
    public static EnemyProjectilePool Instance => s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        InitializePool();
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void InitializePool()
    {
        _available = new Queue<EnemyProjectile>(_initialPoolSize);
        _all = new List<EnemyProjectile>(_initialPoolSize);

        _container = new GameObject("ProjectilePool").transform;
        _container.SetParent(transform);

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateProjectile();
        }
    }

    private EnemyProjectile CreateProjectile(bool enqueue = true)
    {
        var projectile = Instantiate(_projectilePrefab, _container);
        projectile.gameObject.SetActive(false);
        projectile.OnReturnToPool = ReturnProjectile;

        _all.Add(projectile);
        
        if (enqueue)
        {
            _available.Enqueue(projectile);
        }

        return projectile;
    }

    public EnemyProjectile GetProjectile(Vector3 position, Quaternion rotation)
    {
        EnemyProjectile projectile;

        if (_available.Count > 0)
        {
            projectile = _available.Dequeue();
        }
        else
        {
            projectile = CreateProjectile(enqueue: false);
        }

        projectile.transform.SetParent(null);
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);

        return projectile;
    }

    private void ReturnProjectile(EnemyProjectile projectile)
    {
        if (projectile == null) return;

        projectile.gameObject.SetActive(false);
        projectile.transform.SetParent(_container);
        _available.Enqueue(projectile);
    }

    public void ReturnAllProjectiles()
    {
        foreach (var projectile in _all)
        {
            if (projectile != null && projectile.gameObject.activeSelf)
            {
                ReturnProjectile(projectile);
            }
        }
    }
}
