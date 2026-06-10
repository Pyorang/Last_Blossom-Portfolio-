using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SkillVFX : MonoBehaviour
{
    private const string HitSkillProjectile = "적_피격_E스킬투사체";
    private const int SkillHitMaxConcurrent = 10;

    [Header("Movement")]
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _speed = 15f;

    [Header("Damage")]
    [SerializeField] private int _maxHitCount = 6;
    [SerializeField] private float _hitInterval = 0.3f;

    [Header("Suction")]
    [SerializeField] private float _suctionForce = 10f;

    private Vector3 _moveDirection;
    private float _attackPower;
    private float _skillCoefficient;
    private float _critRate;
    private float _critDamage;
    private float _justDamageBonus;
    private bool _isAwakened;
    private Action _onHitCallback;
    private bool _hasSuction;
    private Action _onDamageCallback;

    private Action<Vector3, bool> _onHitVFXCallback;

    private bool _hasPiercing;
    private int _pierceCount;
    private const float PIERCE_DAMAGE_INCREASE = 0.30f;
    private const int MAX_PIERCE_STACKS = 6;

    private float _deactivateTimer;
    private bool _isActive;

    private WaitForSeconds _hitIntervalWait;

    private Dictionary<Collider, Coroutine> _damageCoroutines = new Dictionary<Collider, Coroutine>();

    private void Awake()
    {
        _hitIntervalWait = new WaitForSeconds(_hitInterval);
    }

    public void Activate(Vector3 direction, float attackPower, float skillCoefficient, float critRate, float critDamage, float justDamageBonus, bool isAwakened, Action onHitCallback = null, bool hasSuction = false, Action onDamageCallback = null, bool hasPiercing = false, Action<Vector3, bool> onHitVFXCallback = null)
    {
        _moveDirection = direction;
        _attackPower = attackPower;
        _skillCoefficient = skillCoefficient;
        _critRate = critRate;
        _critDamage = critDamage;
        _justDamageBonus = justDamageBonus;
        _isAwakened = isAwakened;
        _onHitCallback = onHitCallback;
        _hasSuction = hasSuction;
        _onDamageCallback = onDamageCallback;
        _hasPiercing = hasPiercing;
        _pierceCount = 0;
        _onHitVFXCallback = onHitVFXCallback;

        transform.localScale = _hasSuction ? Vector3.one * 2f : Vector3.one;

        _isActive = true;
        _deactivateTimer = _lifeTime;
    }

    private void Update()
    {
        if (!_isActive) return;

        transform.position += _moveDirection * _speed * Time.deltaTime;

        _deactivateTimer -= Time.deltaTime;
        if (_deactivateTimer <= 0f)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        if (!other.TryGetComponent<IDamageable>(out var damageable)) return;

        float pierceMultiplier = 1f;
        if (_hasPiercing)
        {
            pierceMultiplier = 1f + (_pierceCount * PIERCE_DAMAGE_INCREASE);
            _pierceCount = Mathf.Min(_pierceCount + 1, MAX_PIERCE_STACKS);
        }

        if (!_damageCoroutines.ContainsKey(other))
        {
            Coroutine coroutine = StartCoroutine(DamageOverTimeCoroutine(other, damageable, pierceMultiplier));
            _damageCoroutines.Add(other, coroutine);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StopDamageCoroutine(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_hasSuction) return;
        if (other.CompareTag("Player")) return;
        if (!other.TryGetComponent<IDamageable>(out _)) return;
        
        if (other.TryGetComponent<EnemyController>(out var enemy) && enemy.EnemyType == EnemyType.Tank)
            return;

        Vector3 direction = (transform.position - other.transform.position).normalized;
        other.transform.position += direction * _suctionForce * Time.deltaTime;
    }

    private IEnumerator DamageOverTimeCoroutine(Collider target, IDamageable damageable, float pierceMultiplier = 1f)
    {
        int hitCount = 0;

        while (hitCount < _maxHitCount)
        {
            float damage = _attackPower * _skillCoefficient * pierceMultiplier;

            bool isCritical = UnityEngine.Random.value <= _critRate;
            if (isCritical)
            {
                damage *= _critDamage;
            }

            if (_isAwakened)
            {
                damage *= (1f + _justDamageBonus);
            }

            damageable.TakeDamage(damage, true, isCritical);

            _onHitCallback?.Invoke();
            _onDamageCallback?.Invoke();
            _onHitVFXCallback?.Invoke(target.transform.position, true);

            AudioManager.Instance.PlaySFX3D(HitSkillProjectile, target.transform.position, SkillHitMaxConcurrent);

            if (hitCount == 0)
            {
                TimeScaleManager.Instance?.PlayMediumHitStop();
            }
            CameraController.Instance?.Shake(targetId: target.GetInstanceID());

            hitCount++;
            yield return _hitIntervalWait;
        }

        _damageCoroutines.Remove(target);
    }

    private void StopDamageCoroutine(Collider target)
    {
        if (_damageCoroutines.TryGetValue(target, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            _damageCoroutines.Remove(target);
        }
    }

    private void Deactivate()
    {
        _isActive = false;

        foreach (var coroutine in _damageCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _damageCoroutines.Clear();

        SkillPool.Instance.Return(this);
    }

    private void OnDisable()
    {
        _isActive = false;
        _onHitCallback = null;
        _onDamageCallback = null;
        _onHitVFXCallback = null;
        _hasSuction = false;
        _hasPiercing = false;
        _pierceCount = 0;
        transform.localScale = Vector3.one;
        StopAllCoroutines();
        _damageCoroutines.Clear();
    }
}
