using System;

[Serializable]
public struct CharacterStatsModel
{
    public string ID;
    
    public float MaxHP;
    public float HPRegen;
    
    public float MaxEnergy;
    public float EnergyRegen;
    public float HitEnergyRecovery;
    
    public float MaxAwakening;
    public float HitAwakeningRecovery;
    public float AwakeningDuration;
    
    public float Attack;
    public float CritRate;
    public float CritDamage;
    public float JustDamageBonus;
    
    public float EUse;
    public float QUse;
}
