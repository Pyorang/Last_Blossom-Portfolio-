using UnityEngine;

public class RangedAggroBehavior : IAggroBehavior
{
    public Transform EvaluateTarget(EnemyAggroContext context)
    {
        // 플레이어 범위 내 + 벽 없음 → Player
        if (context.Player != null && 
            IsInRange(context.Self, context.Player, context.AttackRange) &&
            context.HasLineOfSight != null && context.HasLineOfSight(context.Player))
        {
            return context.Player;
        }
        
        // 아니면 → SpiritTree
        return context.SpiritTree;
    }
    
    private bool IsInRange(Transform self, Transform target, float range)
    {
        if (self == null || target == null) return false;
        
        Vector3 toTarget = target.position - self.position;
        toTarget.y = 0f;
        
        return toTarget.sqrMagnitude <= range * range;
    }
}
