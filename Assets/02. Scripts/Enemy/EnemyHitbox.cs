using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayerMask;
    
    private float _damage;
    private Collider _collider;
    private bool _isAttacking;
    private HashSet<IDamageable> _hitTargetsThisAttack = new HashSet<IDamageable>();
    private EnemyHitbox[] _siblingHitboxes;
    
    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    private void OnEnable()
    {
        _isAttacking = false;
        _hitTargetsThisAttack.Clear();
    }

    private void Start()
    {
        FindSiblingHitboxes();
    }

    private void FindSiblingHitboxes()
    {
        var enemyAttack = GetComponentInParent<EnemyAttack>();
        if (enemyAttack != null)
        {
            _siblingHitboxes = enemyAttack.GetComponentsInChildren<EnemyHitbox>();
            return;
        }

        var tankAttack = GetComponentInParent<TankAttack>();
        if (tankAttack != null)
        {
            _siblingHitboxes = tankAttack.GetComponentsInChildren<EnemyHitbox>();
        }
    }
    
    public void Initialize(float damage)
    {
        _damage = damage;
    }

    public void SetAttacking(bool isAttacking)
    {
        _isAttacking = isAttacking;
        if (isAttacking)
        {
            _hitTargetsThisAttack.Clear();
        }
    }

    private void MarkAsHit(IDamageable target)
    {
        _hitTargetsThisAttack.Add(target);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider other)
    {
        if (!_isAttacking)
        {
            return;
        }
        
        if (((1 << other.gameObject.layer) & _targetLayerMask) == 0)
        {
            return;
        }
        
        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable = other.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
        {
            return;
        }
        
        if (_hitTargetsThisAttack.Contains(damageable))
        {
            return;
        }
        
        damageable.TakeDamage(_damage);
        MarkAllSiblingsAsHit(damageable);
    }

    private void MarkAllSiblingsAsHit(IDamageable target)
    {
        if (_siblingHitboxes == null)
        {
            return;
        }

        foreach (var hitbox in _siblingHitboxes)
        {
            hitbox.MarkAsHit(target);
        }
    }
}
