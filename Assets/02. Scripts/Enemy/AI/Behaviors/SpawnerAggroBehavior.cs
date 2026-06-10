using UnityEngine;

public class SpawnerAggroBehavior : IAggroBehavior
{
    public Transform EvaluateTarget(EnemyAggroContext context)
    {
        return context.SpiritTree;
    }
}
