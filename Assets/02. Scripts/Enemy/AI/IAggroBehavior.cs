using System;
using UnityEngine;

public interface IAggroBehavior
{
    Transform EvaluateTarget(EnemyAggroContext context);
}

public struct EnemyAggroContext
{
    public Transform Self;
    public Transform Player;
    public Transform SpiritTree;
    public Transform CurrentTarget;
    public float AggroRange;
    public float AggroReleaseRange;
    public bool IsAttacking;
    
    // 어그로 상태
    public bool IsLockedToSpiritTree;
    public bool IsAggroedByDamage;
    public Action ClearDamageAggro;
    
    // 슬롯 관리용
    public EnemyController Controller;
    
    // 원거리용
    public float AttackRange;
    public Func<Transform, bool> HasLineOfSight;
}
