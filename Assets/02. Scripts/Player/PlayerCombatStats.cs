using UnityEngine;

public class PlayerCombatStats : MonoBehaviour
{
    private float _attackPower;
    private float _critRate;
    private float _critDamage;
    private float _justDamageBonus;
    private float _hitEnergyRecovery;
    
    private float _attackPercent;
    private float _damageIncreasePercent;
    
    private float _normalAttack1Coefficient;
    private float _normalAttack2Coefficient;
    private float _normalAttack3Coefficient;
    private float _eSkillCoefficient;
    private float _ultimateSlashCoefficient;
    private float _ultimateStormCoefficient;

    public float AttackPower => _attackPower;
    public float CritRate => _critRate;
    public float CritDamage => _critDamage;
    public float JustDamageBonus => _justDamageBonus;
    public float HitEnergyRecovery => _hitEnergyRecovery;
    public float AttackPercent => _attackPercent;
    public float DamageIncreasePercent => _damageIncreasePercent;
    
    public float NormalAttack1Coefficient => _normalAttack1Coefficient;
    public float NormalAttack2Coefficient => _normalAttack2Coefficient;
    public float NormalAttack3Coefficient => _normalAttack3Coefficient;
    public float ESkillCoefficient => _eSkillCoefficient;
    public float UltimateSlashCoefficient => _ultimateSlashCoefficient;
    public float UltimateStormCoefficient => _ultimateStormCoefficient;

    public void Initialize(float attack, float critRate, float critDamage, float justDamageBonus, float hitEnergyRecovery)
    {
        _attackPower = attack;
        _critRate = critRate / 100f;
        _critDamage = critDamage / 100f;
        _justDamageBonus = justDamageBonus / 100f;
        _hitEnergyRecovery = hitEnergyRecovery;
        
        _attackPercent = 0f;
        _damageIncreasePercent = 0f;
        
        InitializeCoefficients();
    }

    private void InitializeCoefficients()
    {
        _normalAttack1Coefficient = DataTableManager.Instance.GetCoefficient("NormalAttack1");
        _normalAttack2Coefficient = DataTableManager.Instance.GetCoefficient("NormalAttack2");
        _normalAttack3Coefficient = DataTableManager.Instance.GetCoefficient("NormalAttack3");
        _eSkillCoefficient = DataTableManager.Instance.GetCoefficient("ESkill");
        _ultimateSlashCoefficient = DataTableManager.Instance.GetCoefficient("UltimateSlash");
        _ultimateStormCoefficient = DataTableManager.Instance.GetCoefficient("UltimateStorm");
    }

    public void SetAttackPercent(float value) => _attackPercent = value;
    public void AddCritRate(float value) => _critRate += value;
    public void AddCritDamage(float value) => _critDamage += value;
    public void SetJustDamageBonus(float value) => _justDamageBonus = value;
    public void SetESkillCoefficient(float value) => _eSkillCoefficient = value;
    public void MultiplyHitEnergyRecovery(float multiplier) => _hitEnergyRecovery *= multiplier;
    public void SetHitEnergyRecovery(float value) => _hitEnergyRecovery = value;

    public float CalculateDamage(float coefficient, bool isEnhanced, out bool isCritical)
    {
        float baseAttack = _attackPower * (1f + _attackPercent);
        float damage = baseAttack * coefficient * (1f + _damageIncreasePercent);

        isCritical = Random.value <= _critRate;
        if (isCritical)
        {
            damage *= _critDamage;
        }

        if (isEnhanced)
        {
            damage *= (1f + _justDamageBonus);
        }

        return damage;
    }
}
