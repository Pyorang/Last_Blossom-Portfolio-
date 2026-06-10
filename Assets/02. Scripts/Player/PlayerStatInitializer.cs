using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnergyComponent))]
[RequireComponent(typeof(AwakeningComponent))]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerStatInitializer : MonoBehaviour
{
    private const string PlayerId = "Player";

    [Header("UI")]
    [SerializeField] private InGameUIController _uiController;

    private HealthComponent _healthComponent;
    private EnergyComponent _energyComponent;
    private AwakeningComponent _awakeningComponent;
    private PlayerAttack _playerAttack;

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
        _energyComponent = GetComponent<EnergyComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
        _playerAttack = GetComponent<PlayerAttack>();
    }

    private void Start()
    {
        if (DataTableManager.Instance.IsInitialized)
        {
            InitializeStats();
        }
        else
        {
            DataTableManager.Instance.OnInitialized += InitializeStats;
        }
    }

    private void OnDestroy()
    {
        if (DataTableManager.Instance != null)
        {
            DataTableManager.Instance.OnInitialized -= InitializeStats;
        }

        if (_healthComponent != null)
        {
            if (_uiController != null) _healthComponent.OnHealthChanged -= _uiController.UpdateHealthUI;
            _healthComponent.OnDeath -= HandlePlayerDeath;
        }

        if (_energyComponent != null && _uiController != null)
        {
            _energyComponent.OnEnergyChanged -= _uiController.UpdateStaminaUI;
            _energyComponent.OnEnergyChanged -= UpdateSkillIcons;
        }

        if (_awakeningComponent != null && _uiController != null)
        {
            _awakeningComponent.OnAwakeningChanged -= _uiController.UpdateAscendUI;
        }

        if (_playerAttack != null && _uiController != null)
        {
            _playerAttack.OnUltimateUnlocked -= _uiController.UnlockUltimateUI;
        }
    }

    private void InitializeStats()
    {
        var stats = DataTableManager.Instance.GetCharacterStats(PlayerId);

        _playerAttack.InitializeStats(stats.Attack, stats.CritRate, stats.CritDamage, stats.MaxAwakening, stats.EUse, stats.QUse, stats.JustDamageBonus, stats.HitEnergyRecovery);

        if (_uiController != null)
        {
            _healthComponent.OnHealthChanged += _uiController.UpdateHealthUI;
            _energyComponent.OnEnergyChanged += _uiController.UpdateStaminaUI;
            _energyComponent.OnEnergyChanged += UpdateSkillIcons;
            _awakeningComponent.OnAwakeningChanged += _uiController.UpdateAscendUI;
            _playerAttack.OnUltimateUnlocked += _uiController.UnlockUltimateUI;
        }

        _healthComponent.OnDeath += HandlePlayerDeath;

        _healthComponent.Initialize(stats.MaxHP, stats.HPRegen);
        _energyComponent.Initialize(stats.MaxEnergy, stats.EnergyRegen);
        _awakeningComponent.Initialize(stats.MaxAwakening, stats.HitAwakeningRecovery, stats.AwakeningDuration);

    }

    private void UpdateSkillIcons(float currentEnergy, float maxEnergy)
    {
        if (_uiController == null) return;

        _uiController.SetSkillIconState(SkillType.ESkill, currentEnergy >= _playerAttack.ESkillEnergyCost);
        _uiController.SetSkillIconState(SkillType.QSkill, currentEnergy >= _playerAttack.QSkillEnergyCost);
    }

    private void HandlePlayerDeath()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.FailWave();
        }
    }
}
