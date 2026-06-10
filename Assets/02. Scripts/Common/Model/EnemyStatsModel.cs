using System;

[Serializable]
public struct EnemyStatsModel
{
    public string ID;
    public float MaxHP;
    public float AttackDamage;
    public float AttackCooldown;
    public float MoveSpeed;
    public float RotationSpeed;
    public float AggroRange;
    public float AggroReleaseRange;
    public float PlayerStoppingDistance;
    public float SpiritTreeStoppingDistance;

    public EnemyType GetEnemyType()
    {
        if (string.IsNullOrEmpty(ID)) return EnemyType.Melee;

        string lowerID = ID.ToLowerInvariant();

        if (lowerID.Contains("ranged")) return EnemyType.Ranged;
        if (lowerID.Contains("kamikaze")) return EnemyType.Kamikaze;
        if (lowerID.Contains("spawner")) return EnemyType.Spawner;
        if (lowerID.Contains("tank")) return EnemyType.Tank;

        return EnemyType.Melee;
    }
}
