using System;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private const float GroundCheckDistance = 0.5f;

    [Header("투사체 설정")]
    [SerializeField] private float _directSpeed = 15f;
    [SerializeField] private float _arcDuration = 1.5f;
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _groundMask;

    [Header("충돌 설정")]
    [SerializeField] private float _collisionRadius = 0.5f;
    [SerializeField] private float _groundImpactRadius = 1.5f;
    [SerializeField] private LayerMask _wallMask;

    [Header("호밍 설정")]
    [SerializeField] private float _homingSpeed = 3f;
    [SerializeField] private float _homingRotateSpeed = 90f;
    [SerializeField] private float _homingDuration = 5f;
    [SerializeField] private float _explosionRadius = 2f;

    [Header("직선 발사 설정")]
    [SerializeField] private float _directArcHeight = 0.5f;

    [Header("곡사 발사 설정")]
    [SerializeField] private float _minArcHeight = 3f;
    [SerializeField] private float _maxArcHeight = 10f;
    [SerializeField] private float _arcHeightPerDistance = 0.4f;

    [Header("이펙트 설정")]
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private GameObject _flashEffectPrefab;

    [Header("사운드 설정")]
    [SerializeField] private string _hitSoundName = "산성피격";

    private float _damage;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _elapsedTime;
    private float _currentArcHeight;
    private float _currentDuration;
    private bool _isHighArc;
    private bool _isActive;
    private Transform _cachedTransform;

    // 호밍용
    private Transform _homingTarget;
    private bool _isHoming;

    private static readonly Collider[] s_hitBuffer = new Collider[16];

    public Action<EnemyProjectile> OnReturnToPool;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    private void OnEnable()
    {
        _elapsedTime = 0f;
        _isActive = false;
        _isHoming = false;
        _homingTarget = null;
    }

    private void OnDisable()
    {
        _isActive = false;
        _isHoming = false;
    }

    private void Update()
    {
        if (!_isActive) return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _lifetime)
        {
            if (_isHoming)
            {
                Explode();
            }
            else
            {
                ReturnToPool();
            }
            return;
        }

        if (_isHoming)
        {
            UpdateHoming();
        }
        else
        {
            UpdateMovement();
        }
    }

    public void LaunchDirect(Vector3 targetPosition, float damage)
    {
        _damage = damage;
        _isHighArc = false;
        _isActive = true;
        _startPosition = _cachedTransform.position;
        _targetPosition = targetPosition;

        float distance = Vector3.Distance(_startPosition, _targetPosition);
        _currentDuration = distance / _directSpeed;
        _currentArcHeight = _directArcHeight;

        LookAtTarget();
        SpawnFlashEffect();
    }

    public void LaunchArc(Vector3 targetPosition, float damage)
    {
        _damage = damage;
        _isHighArc = true;
        _isActive = true;
        _startPosition = _cachedTransform.position;
        _targetPosition = targetPosition;

        float distance = Vector3.Distance(_startPosition, _targetPosition);
        _currentArcHeight = Mathf.Clamp(distance * _arcHeightPerDistance, _minArcHeight, _maxArcHeight);
        _currentDuration = _arcDuration;

        LookAtTarget();
        SpawnFlashEffect();
    }

    public void LaunchHoming(Transform target, float damage)
    {
        _damage = damage;
        _homingTarget = target;
        _isHoming = true;
        _isHighArc = false;
        _isActive = true;
        _elapsedTime = 0f;
        _lifetime = _homingDuration;

        if (target != null)
        {
            Vector3 direction = (target.position - _cachedTransform.position).normalized;
            if (direction.sqrMagnitude > 0.0001f)
            {
                _cachedTransform.rotation = Quaternion.LookRotation(direction);
            }
        }

        SpawnFlashEffect();
    }

    private void UpdateHoming()
    {
        if (_homingTarget == null)
        {
            Explode();
            return;
        }

        // 타겟 방향으로 회전
        Vector3 targetPos = _homingTarget.position + Vector3.up * 1f;
        Vector3 direction = (targetPos - _cachedTransform.position).normalized;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _cachedTransform.rotation = Quaternion.RotateTowards(
                _cachedTransform.rotation,
                targetRotation,
                _homingRotateSpeed * Time.deltaTime
            );
        }

        // 전방으로 이동
        Vector3 previousPosition = _cachedTransform.position;
        Vector3 newPosition = previousPosition + _cachedTransform.forward * _homingSpeed * Time.deltaTime;

        // 타겟 직접 충돌 체크
        if (CheckDirectHit(previousPosition, newPosition)) return;

        _cachedTransform.position = newPosition;
    }

    private void Explode()
    {
        // 범위 내 타겟에게 데미지
        int hitCount = Physics.OverlapSphereNonAlloc(_cachedTransform.position, _explosionRadius, s_hitBuffer, _targetMask);
        for (int i = 0; i < hitCount; i++)
        {
            TryApplyDamage(s_hitBuffer[i]);
        }

        SpawnHitEffect(_cachedTransform.position, Vector3.up);
        ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive || !_isHoming) return;

        // 벽/바닥 충돌 → 폭발
        int layer = 1 << other.gameObject.layer;
        if ((layer & _groundMask) != 0 || (layer & _wallMask) != 0)
        {
            Explode();
        }
    }

    private void UpdateMovement()
    {
        float t = _elapsedTime / _currentDuration;

        if (t >= 1f)
        {
            _cachedTransform.position = _targetPosition;
            OnImpact(_targetPosition);
            return;
        }

        Vector3 previousPosition = _cachedTransform.position;
        Vector3 newPosition = CalculatePosition(t);

        if (CheckDirectHit(previousPosition, newPosition)) return;

        if (CheckGroundImpact(newPosition))
        {
            OnImpact(newPosition);
            return;
        }

        _cachedTransform.position = newPosition;
        UpdateRotation(previousPosition, newPosition);
    }

    private Vector3 CalculatePosition(float t)
    {
        Vector3 horizontalPosition = Vector3.Lerp(_startPosition, _targetPosition, t);
        float baseHeight = Mathf.Lerp(_startPosition.y, _targetPosition.y, t);
        float arcOffset = 4f * _currentArcHeight * t * (1f - t);
        return new Vector3(horizontalPosition.x, baseHeight + arcOffset, horizontalPosition.z);
    }

    private void UpdateRotation(Vector3 previousPosition, Vector3 newPosition)
    {
        Vector3 velocity = newPosition - previousPosition;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            _cachedTransform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    private void LookAtTarget()
    {
        Vector3 direction = (_targetPosition - _startPosition).normalized;

        if (_isHighArc)
        {
            direction = Quaternion.Euler(-45f, 0f, 0f) * new Vector3(direction.x, 0f, direction.z).normalized;
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            _cachedTransform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private bool CheckDirectHit(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.001f) return false;

        // 벽 충돌 체크
        if (Physics.SphereCast(from, _collisionRadius, direction.normalized, out var wallHit, distance, _wallMask))
        {
            SpawnHitEffect(wallHit.point, wallHit.normal);
            ReturnToPool();
            return true;
        }

        // 타겟 충돌 체크
        if (Physics.SphereCast(from, _collisionRadius, direction.normalized, out var hit, distance, _targetMask))
        {
            if (TryApplyDamage(hit.collider))
            {
                SpawnHitEffect(hit.point, hit.normal);
                ReturnToPool();
                return true;
            }
        }
        return false;
    }

    private bool CheckGroundImpact(Vector3 position)
    {
        if (position.y > _targetPosition.y + 0.5f) return false;
        return Physics.Raycast(position, Vector3.down, GroundCheckDistance, _groundMask);
    }

    private void OnImpact(Vector3 position)
    {
        ApplyDamageAtPosition(position);
        SpawnHitEffect(position, Vector3.up);
        ReturnToPool();
    }

    private bool TryApplyDamage(Collider collider)
    {
        if (collider == null) return false;

        if (!collider.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable = collider.GetComponentInParent<IDamageable>();
        }

        if (damageable == null) return false;

        var playerSFX = collider.GetComponentInParent<PlayerSFX>();
        playerSFX?.SetAcidDamageSFX();

        damageable.TakeDamage(_damage);
        PlayHitSound(collider.transform.position);
        return true;
    }

    private void PlayHitSound(Vector3 position)
    {
        if (string.IsNullOrEmpty(_hitSoundName)) return;
        AudioManager.Instance?.PlaySFX3D(_hitSoundName, position);
    }

    private void ApplyDamageAtPosition(Vector3 position)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(position, _groundImpactRadius, s_hitBuffer, _targetMask);
        for (int i = 0; i < hitCount; i++)
        {
            TryApplyDamage(s_hitBuffer[i]);
        }
    }

    private void SpawnFlashEffect()
    {
        if (_flashEffectPrefab == null) return;

        var flash = Instantiate(_flashEffectPrefab, _cachedTransform.position, _cachedTransform.rotation);
        var ps = flash.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            Destroy(flash, ps.main.duration);
        }
        else
        {
            Destroy(flash, 2f);
        }
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(normal);
        var hit = Instantiate(_hitEffectPrefab, position, rotation);
        var ps = hit.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            Destroy(hit, ps.main.duration);
        }
        else
        {
            Destroy(hit, 2f);
        }
    }

    private void ReturnToPool()
    {
        _isActive = false;
        _isHoming = false;
        _homingTarget = null;
        OnReturnToPool?.Invoke(this);
    }
}
