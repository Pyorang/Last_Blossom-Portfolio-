using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(PlayerCombatStats))]
[RequireComponent(typeof(PlayerHitDetector))]
[RequireComponent(typeof(PlayerPerkHandler))]
[RequireComponent(typeof(EnergyComponent))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(AwakeningComponent))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerVFX))]
[RequireComponent(typeof(PlayerAwakeningEffect))]
[RequireComponent(typeof(PlayerHitProcessor))]
[RequireComponent(typeof(PlayerSFX))]
public class PlayerAttack : MonoBehaviour
{
    private PlayerAnimator _playerAnimator;
    private PlayerCombatStats _combatStats;
    private PlayerHitDetector _hitDetector;
    private PlayerPerkHandler _perkHandler;
    private EnergyComponent _energyComponent;
    private HealthComponent _healthComponent;
    private AwakeningComponent _awakeningComponent;
    private CharacterController _characterController;
    private PlayerVFX _playerVFX;
    private PlayerAwakeningEffect _awakeningEffect;
    private PlayerHitProcessor _hitProcessor;
    private PlayerSFX _playerSFX;

    private float _eSkillEnergyCost;
    private float _qSkillEnergyCost;
    private float _maxAwakening;

    public bool _isAttacking;
    private bool _canDoNextCombo;
    private bool _isJustTime;
    private bool _hasNextComboInput;
    private bool _isJustSuccess;
    private bool _nextAttackEnhanced;
    private bool _isUsingSkill;
    private bool _isUsingUltimate;
    private bool _isUltimateLocked = true;
    private int _comboCount;
    private const int MAX_COMBO = 3;

    private LayerMask _originalExcludeLayers;

    public float AttackPower => _combatStats.AttackPower;
    public float CritRate => _combatStats.CritRate;
    public float CritDamage => _combatStats.CritDamage;
    public float MaxAwakening => _maxAwakening;
    public float ESkillEnergyCost => _eSkillEnergyCost;
    public float QSkillEnergyCost => _qSkillEnergyCost;

    public bool IsAttacking => _isAttacking;
    public bool IsJustSuccess => _isJustSuccess;
    public bool IsUsingSkill => _isUsingSkill;
    public bool IsUsingUltimate => _isUsingUltimate;
    public bool IsUltimateLocked => _isUltimateLocked;

    public event Action OnUltimateUnlocked;

    [Header("Ultimate")]
    [SerializeField] private UltimateCameraController _ultimateCameraController;
    [SerializeField] private int _ultimateSlashHitCount = 3;
    [SerializeField] private float _ultimateSlashInterval = 0.15f;
    [SerializeField] private int _ultimateStormHitCount = 6;
    [SerializeField] private float _ultimateStormInterval = 0.08f;

    [Header("Settings")]
    [SerializeField] private LayerMask _enemyLayerMask;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _combatStats = GetComponent<PlayerCombatStats>();
        _hitDetector = GetComponent<PlayerHitDetector>();
        _perkHandler = GetComponent<PlayerPerkHandler>();
        _energyComponent = GetComponent<EnergyComponent>();
        _healthComponent = GetComponent<HealthComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
        _characterController = GetComponent<CharacterController>();
        _playerVFX = GetComponent<PlayerVFX>();
        _awakeningEffect = GetComponent<PlayerAwakeningEffect>();
        _hitProcessor = GetComponent<PlayerHitProcessor>();
        _playerSFX = GetComponent<PlayerSFX>();
    }

    private void OnEnable()
    {
        if (_hitDetector != null)
        {
            _hitDetector.OnHitDetected += _hitProcessor.ProcessHits;
        }
    }

    private void OnDisable()
    {
        if (_hitDetector != null)
        {
            _hitDetector.OnHitDetected -= _hitProcessor.ProcessHits;
        }
    }

    public void InitializeStats(float attack, float critRate, float critDamage, float awakening, float eUse, float qUse, float justDamageBonus, float hitEnergyRecovery)
    {
        _maxAwakening = awakening;
        _eSkillEnergyCost = eUse;
        _qSkillEnergyCost = qUse;
        
        _combatStats.Initialize(attack, critRate, critDamage, justDamageBonus, hitEnergyRecovery);
    }

    #region Attack Actions

    public void TryAttack()
    {
        if (!_isAttacking)
        {
            _playerAnimator.PlayAttackAnimation();
            return;
        }

        if (_canDoNextCombo)
        {
            _hasNextComboInput = true;
            if (_isJustTime)
            {
                _nextAttackEnhanced = true;
            }
        }
    }

    public void UseSkill()
    {
        if (_isUsingSkill) return;
        if (!_energyComponent.HasEnoughEnergy(_eSkillEnergyCost)) return;

        _isUsingSkill = true;
        _hitProcessor.ResetHitSFXTracking();
        _playerAnimator.PlaySkillAnimation();
    }

    public void UseUltimate()
    {
        if (_isUsingSkill || _isUltimateLocked) return;
        if (!_energyComponent.HasEnoughEnergy(_qSkillEnergyCost)) return;

        _energyComponent.TryConsumeEnergy(_qSkillEnergyCost);
        _perkHandler.TriggerReturnWindEffect(_qSkillEnergyCost);

        _isUsingSkill = true;
        _isUsingUltimate = true;
        _hitProcessor.ResetHitSFXTracking();
        _healthComponent.SetInvincibleOn();
        _playerVFX.TriggerCherryBlossomVFX();
        _ultimateCameraController?.StartUltimateCamera();
        _playerAnimator.PlayUltimateAnimation();
    }

    #endregion

    #region Animation Event Functions

    public void StartAttack() => _isAttacking = true;

    public void ComboOn()
    {
        _isJustSuccess = _nextAttackEnhanced;
        _nextAttackEnhanced = false;
        _comboCount++;
        
        _hitProcessor.ResetHitSFXTracking();
        _playerVFX.PlayAttackVFX(_awakeningEffect.IsAwakened || _isJustSuccess);

        if (_comboCount >= MAX_COMBO && _comboCount % MAX_COMBO == 0)
        {
            if (_perkHandler.HasWildWind) ExecuteWildWindAttack();
            if (_perkHandler.HasWhirlWind) ExecuteWhirlWindAttack();
        }

        _canDoNextCombo = true;
        _hasNextComboInput = false;
    }

    public void JustOn() => _isJustTime = true;
    public void JustOff() => _isJustTime = false;

    public void ComboOff()
    {
        _canDoNextCombo = false;
        _isJustTime = false;

        if (_hasNextComboInput)
        {
            _hasNextComboInput = false;
            _playerAnimator.PlayAttackAnimation();
        }
        else
        {
            _isJustSuccess = false;
            _nextAttackEnhanced = false;
        }
    }

    public void ResetAttackState()
    {
        if (_isUsingSkill)
        {
            _isUsingSkill = false;
        }

        _isAttacking = false;
        _canDoNextCombo = false;
        _isJustTime = false;
        _hasNextComboInput = false;
        _isJustSuccess = false;
        _nextAttackEnhanced = false;
        _comboCount = 0;
        _hitProcessor.ResetHitSFXTracking();
    }

    public void SpawnSkillVFX()
    {
        if (SkillPool.Instance == null || _playerVFX.SlashLocation == null) return;

        float effectiveAttackPower = _combatStats.AttackPower * (1f + _combatStats.AttackPercent);
        Quaternion vfxRotation = _playerVFX.GetSkillVFXRotation();
        SkillPool.Instance.Spawn(
            _playerVFX.SlashLocation.position, 
            vfxRotation, 
            transform.forward, 
            effectiveAttackPower, 
            _combatStats.ESkillCoefficient, 
            _combatStats.CritRate, 
            _combatStats.CritDamage, 
            _combatStats.JustDamageBonus, 
            _awakeningEffect.IsAwakened, 
            _awakeningComponent.OnHit, 
            _perkHandler.HasSuctionWind, 
            _perkHandler.HasVitalWind ? OnVitalWindHit : null, 
            _perkHandler.HasPiercingWind,
            _playerVFX.PlayHitVFX
        );
    }

    private void OnVitalWindHit()
    {
        _perkHandler.TriggerVitalWindHeal();
    }

    public void ConsumeSkillEnergy()
    {
        _energyComponent.TryConsumeEnergy(_eSkillEnergyCost);
        _perkHandler.TriggerLingeringWind(cost => _eSkillEnergyCost = cost);
    }

    public void FinishUsingSkill()
    {
        if (_isUsingSkill)
        {
            _isUsingSkill = false;
        }
    }

    public void FinishUsingUltimate()
    {
        _healthComponent.SetInvincibleOff();
        _isUsingUltimate = false;
        if (_isUsingSkill)
        {
            _isUsingSkill = false;
        }
    }

    #endregion

    #region Hit Detection Execution

    public void ExecuteNormalAttack1()
    {
        _hitDetector.DetectNormalAttack1(_combatStats.NormalAttack1Coefficient, _awakeningEffect.IsAwakened || _isJustSuccess);
    }

    public void ExecuteNormalAttack2()
    {
        _hitDetector.DetectNormalAttack2(_combatStats.NormalAttack2Coefficient, _awakeningEffect.IsAwakened || _isJustSuccess);
    }

    public void ExecuteNormalAttack3()
    {
        _hitDetector.DetectNormalAttack3(_combatStats.NormalAttack3Coefficient, _awakeningEffect.IsAwakened || _isJustSuccess);
    }

    public void ExecuteUltimateSlash()
    {
        StartCoroutine(PerformMultiHit(_ultimateSlashHitCount, _ultimateSlashInterval, _combatStats.UltimateSlashCoefficient));
    }

    public void ExecuteUltimateStorm()
    {
        int hitCount = _perkHandler.GetTyphoonHitCount(_ultimateStormHitCount);
        float interval = _perkHandler.GetTyphoonInterval(_ultimateStormInterval);
        StartCoroutine(PerformMultiHit(hitCount, interval, _combatStats.UltimateStormCoefficient));
    }

    private IEnumerator PerformMultiHit(int hitCount, float interval, float coefficient)
    {
        for (int i = 0; i < hitCount; i++)
        {
            _hitDetector.DetectUltimate(coefficient);
            if (i < hitCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private void ExecuteWildWindAttack()
    {
        int hitCount = _hitDetector.DetectSphere(transform.position + Vector3.up, 4f, out var hits);
        _hitProcessor.ProcessHits(hits, hitCount, _perkHandler.WildWindCoefficient, true, PlayerHitType.Perk);
    }

    private void ExecuteWhirlWindAttack()
    {
        int hitCount = _hitDetector.DetectSphere(transform.position + Vector3.up, 4f, out var hits);
        int actualHits = 0;
        
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = _combatStats.CalculateDamage(_perkHandler.WhirlWindCoefficient, false, out bool isCritical);
                damageable.TakeDamage(damage, false, isCritical);
                actualHits++;
            }
        }

        if (actualHits > 0)
        {
            _playerSFX.PlayHitSFX(PlayerHitType.Perk);
        }
        
        _perkHandler.TriggerWhirlWindHeal(actualHits);
    }

    #endregion

    #region Perk Delegation

    public void UnlockUltimate()
    {
        _isUltimateLocked = false;
        OnUltimateUnlocked?.Invoke();
    }

    public void AddCritRate(float value) => _combatStats.AddCritRate(value);
    public void AddCritDamage(float value) => _combatStats.AddCritDamage(value);
    public void AddHitEnergyRecovery(float percent) => _combatStats.MultiplyHitEnergyRecovery(1f + percent);
    public void SetESkillCoefficient(float value) => _combatStats.SetESkillCoefficient(value);
    public void SetJustDamageBonus(float value) => _combatStats.SetJustDamageBonus(value);

    public void EnableLingeringWind(string perkId) => _perkHandler.EnableLingeringWind(perkId, _eSkillEnergyCost);
    public void EnableWildWind(string perkId) => _perkHandler.EnableWildWind(perkId);
    public void EnableSuctionWind(string perkId) => _perkHandler.EnableSuctionWind(perkId);
    public void EnableVitalWind(string perkId) => _perkHandler.EnableVitalWind(perkId);
    public void EnableWhirlWind(string perkId) => _perkHandler.EnableWhirlWind(perkId);
    public void EnablePiercingWind(string perkId) => _perkHandler.EnablePiercingWind(perkId);
    public void EnableTwinWind(string perkId) => _perkHandler.EnableTwinWind(perkId);
    public void EnableTyphoon(string perkId) => _perkHandler.EnableTyphoon(perkId);
    public void EnableReturnWind(string perkId) => _perkHandler.EnableReturnWind(perkId);

    public bool HasPiercingWind => _perkHandler.HasPiercingWind;

    #endregion

    #region Collision Control

    public void DisableEnemyCollision()
    {
        _originalExcludeLayers = _characterController.excludeLayers;
        _characterController.excludeLayers = _originalExcludeLayers | _enemyLayerMask;
    }

    public void EnableEnemyCollision()
    {
        _characterController.excludeLayers = _originalExcludeLayers;
    }

    #endregion
}
