using UnityEngine;

public class TankAggroBehavior : IAggroBehavior
{
    public Transform EvaluateTarget(EnemyAggroContext context)
    {
        if (context.Player == null)
        {
            return context.SpiritTree;
        }
        
        float sqrDistanceToPlayer = (context.Player.position - context.Self.position).sqrMagnitude;
        float sqrAggroRelease = context.AggroReleaseRange * context.AggroReleaseRange;
        
        // 1. 피격 어그로 상태 (최우선)
        if (context.IsAggroedByDamage)
        {
            if (sqrDistanceToPlayer > sqrAggroRelease)
            {
                context.ClearDamageAggro?.Invoke();
                return context.SpiritTree;
            }
            return context.Player;
        }
        
        // 2. 영목 공격 잠금 상태
        if (context.IsLockedToSpiritTree)
        {
            return context.SpiritTree;
        }
        
        // 3. 기존 거리 기반 로직
        if (context.CurrentTarget == context.Player)
        {
            if (sqrDistanceToPlayer > sqrAggroRelease)
            {
                return context.SpiritTree;
            }
            return context.Player;
        }

        if (sqrDistanceToPlayer <= context.AggroRange * context.AggroRange)
        {
            return context.Player;
        }

        return context.SpiritTree;
    }
}
