public static class AggroBehaviorFactory
{
    private static readonly MeleeAggroBehavior s_meleeBehavior = new();
    private static readonly KamikazeAggroBehavior s_kamikazeBehavior = new();
    private static readonly SpawnerAggroBehavior s_spawnerBehavior = new();
    private static readonly TankAggroBehavior s_tankBehavior = new();
    private static readonly RangedAggroBehavior s_rangedBehavior = new();

    public static IAggroBehavior Create(EnemyType type)
    {
        return type switch
        {
            EnemyType.Melee => s_meleeBehavior,
            EnemyType.Kamikaze => s_kamikazeBehavior,
            EnemyType.Spawner => s_spawnerBehavior,
            EnemyType.Tank => s_tankBehavior,
            EnemyType.Ranged => s_rangedBehavior,
            _ => s_meleeBehavior
        };
    }
}
