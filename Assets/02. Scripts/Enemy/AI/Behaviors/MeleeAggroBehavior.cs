using UnityEngine;

public class MeleeAggroBehavior : IAggroBehavior
{
    public Transform EvaluateTarget(EnemyAggroContext context)
    {
        if (context.Player == null)
        {
            return context.SpiritTree;
        }
        
        float sqrDistanceToPlayer = (context.Player.position - context.Self.position).sqrMagnitude;
        float sqrAggroRelease = context.AggroReleaseRange * context.AggroReleaseRange;
        
        // 1. 피격 어그로 상태
        if (context.IsAggroedByDamage)
        {
            // 거리 벗어남 → 어그로 해제
            if (sqrDistanceToPlayer > sqrAggroRelease)
            {
                context.ClearDamageAggro?.Invoke();
                return context.SpiritTree;
            }
            
            // 슬롯 획득 시도
            if (TryAcquireSlot(context))
            {
                return context.Player;
            }
            
            // 슬롯 없으면 어그로 해제하고 영목 계속
            context.ClearDamageAggro?.Invoke();
            return context.SpiritTree;
        }
        
        // 2. 영목 공격 잠금 상태
        if (context.IsLockedToSpiritTree)
        {
            return context.SpiritTree;
        }
        
        // 3. 이미 플레이어 타겟 중 (슬롯 보유)
        if (context.CurrentTarget == context.Player)
        {
            // 거리 벗어나면 슬롯 반납
            if (sqrDistanceToPlayer > sqrAggroRelease)
            {
                ReleaseSlot(context);
                return context.SpiritTree;
            }
            return context.Player;
        }

        // 4. 거리 기반 어그로 (새로 플레이어 감지)
        if (sqrDistanceToPlayer <= context.AggroRange * context.AggroRange)
        {
            if (TryAcquireSlot(context))
            {
                return context.Player;
            }
        }

        return context.SpiritTree;
    }
    
    private bool TryAcquireSlot(EnemyAggroContext context)
    {
        if (MeleeAttackCoordinator.Instance == null)
            return true; // Coordinator 없으면 제한 없음
            
        return MeleeAttackCoordinator.Instance.TryAcquireSlot(context.Controller);
    }
    
    private void ReleaseSlot(EnemyAggroContext context)
    {
        MeleeAttackCoordinator.Instance?.ReleaseSlot(context.Controller);
    }
}
