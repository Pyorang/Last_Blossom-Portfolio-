using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    [SerializeField] private float _duration = 2f;
    
    private SpawnEffectPool _pool;
    private float _timer;

    public void Initialize(SpawnEffectPool pool)
    {
        _pool = pool;
    }
    
    public void SetDuration(float duration)
    {
        _duration = duration;
    }

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= _duration)
        {
            _pool?.Return(gameObject);
        }
    }
}
