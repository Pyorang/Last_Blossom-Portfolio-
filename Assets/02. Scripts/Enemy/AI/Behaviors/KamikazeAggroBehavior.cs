using UnityEngine;

public class KamikazeAggroBehavior : IAggroBehavior
{
    public Transform EvaluateTarget(EnemyAggroContext context)
    {
        return context.SpiritTree;
    }
}
