using UnityEngine;

[RequireComponent(typeof(AwakeningComponent))]
[RequireComponent(typeof(PlayerCombatStats))]
[RequireComponent(typeof(EnergyComponent))]
public class PlayerAwakeningEffect : MonoBehaviour
{
    private const float ATTACK_PERCENT_BONUS = 0.25f;
    private const float HIT_ENERGY_RECOVERY_MULTIPLIER = 1.5f;
    private const float ENERGY_REGEN_MULTIPLIER = 1.5f;

    [SerializeField] private InGameUIController _uiController;

    private AwakeningComponent _awakeningComponent;
    private PlayerCombatStats _combatStats;
    private EnergyComponent _energyComponent;

    private float _originalHitEnergyRecovery;
    private float _originalEnergyRegen;

    public bool IsAwakened { get; private set; }

    private void Awake()
    {
        _awakeningComponent = GetComponent<AwakeningComponent>();
        _combatStats = GetComponent<PlayerCombatStats>();
        _energyComponent = GetComponent<EnergyComponent>();
    }

    private void OnEnable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated += ActivateEffects;
            _awakeningComponent.OnAwakeningDeactivated += DeactivateEffects;
        }
    }

    private void OnDisable()
    {
        if (_awakeningComponent != null)
        {
            _awakeningComponent.OnAwakeningActivated -= ActivateEffects;
            _awakeningComponent.OnAwakeningDeactivated -= DeactivateEffects;
        }
    }

    private void ActivateEffects()
    {
        IsAwakened = true;
        _combatStats.SetAttackPercent(ATTACK_PERCENT_BONUS);

        _originalHitEnergyRecovery = _combatStats.HitEnergyRecovery;
        _combatStats.SetHitEnergyRecovery(_originalHitEnergyRecovery * HIT_ENERGY_RECOVERY_MULTIPLIER);

        _originalEnergyRegen = _energyComponent.EnergyRegenPerSecond;
        _energyComponent.SetEnergyRegen(_originalEnergyRegen * ENERGY_REGEN_MULTIPLIER);
        
        _uiController?.SetAwakenedProfile();
    }

    private void DeactivateEffects()
    {
        IsAwakened = false;
        _combatStats.SetAttackPercent(0f);

        _combatStats.SetHitEnergyRecovery(_originalHitEnergyRecovery);
        _energyComponent.SetEnergyRegen(_originalEnergyRegen);
        
        _uiController?.SetNormalProfile();
    }
}
