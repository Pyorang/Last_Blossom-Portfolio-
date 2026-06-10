using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPerkHandler : MonoBehaviour
{
    private PlayerAnimator _playerAnimator;
    private EnergyComponent _energyComponent;
    private HealthComponent _healthComponent;
    private AwakeningComponent _awakeningComponent;

    private HashSet<string> _ownedPerkIds = new HashSet<string>();
    public IReadOnlyCollection<string> OwnedPerkIds => _ownedPerkIds;

    public void RegisterPerk(string perkId)
    {
        _ownedPerkIds.Add(perkId);
    }

    private bool _hasLingeringWind;
    private bool _hasWildWind;
    private bool _hasSuctionWind;
    private bool _hasVitalWind;
    private bool _hasWhirlWind;
    private bool _hasPiercingWind;
    private bool _hasTyphoon;
    private bool _hasReturnWind;

    private const float LINGERING_WIND_DURATION = 3f;
    private const float LINGERING_WIND_DISCOUNT = 0.5f;
    private const float WILD_WIND_BONUS_COEFFICIENT = 1.0f;
    private const float VITAL_WIND_HEAL_PERCENT = 0.005f;
    private const float WHIRL_WIND_COEFFICIENT = 0.5f;
    private const float WHIRL_WIND_HEAL_PERCENT = 0.02f;
    private const float RETURN_WIND_ENERGY_REFUND = 0.5f;
    private const float RETURN_WIND_AWAKENING_GAIN = 30f;

    private float _originalESkillEnergyCost;
    private Coroutine _lingeringWindCoroutine;

    public bool HasSuctionWind => _hasSuctionWind;
    public bool HasVitalWind => _hasVitalWind;
    public bool HasPiercingWind => _hasPiercingWind;
    public bool HasTyphoon => _hasTyphoon;
    public bool HasReturnWind => _hasReturnWind;
    public bool HasWildWind => _hasWildWind;
    public bool HasWhirlWind => _hasWhirlWind;

    public float WildWindCoefficient => WILD_WIND_BONUS_COEFFICIENT;
    public float WhirlWindCoefficient => WHIRL_WIND_COEFFICIENT;
    public float ReturnWindEnergyRefund => RETURN_WIND_ENERGY_REFUND;
    public float ReturnWindAwakeningGain => RETURN_WIND_AWAKENING_GAIN;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _energyComponent = GetComponent<EnergyComponent>();
        _healthComponent = GetComponent<HealthComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
    }

    #region Enable Methods
    
    public void EnableLingeringWind(string perkId, float originalEnergyCost)
    {
        _hasLingeringWind = true;
        _originalESkillEnergyCost = originalEnergyCost;
        _ownedPerkIds.Add(perkId);
    }

    public void EnableWildWind(string perkId)
    {
        _hasWildWind = true;
        _ownedPerkIds.Add(perkId);
    }

    public void EnableSuctionWind(string perkId)
    {
        _hasSuctionWind = true;
        _ownedPerkIds.Add(perkId);
    }

    public void EnableVitalWind(string perkId)
    {
        _hasVitalWind = true;
        _ownedPerkIds.Add(perkId);
    }

    public void EnableWhirlWind(string perkId)
    {
        _hasWhirlWind = true;
        _ownedPerkIds.Add(perkId);
    }

    public void EnablePiercingWind(string perkId)
    {
        _hasPiercingWind = true;
        _ownedPerkIds.Add(perkId);
    }
    
    public void EnableTwinWind(string perkId)
    {
        _playerAnimator.SetTwinWind(true);
        _ownedPerkIds.Add(perkId);
    }
    
    public void EnableTyphoon(string perkId)
    {
        _hasTyphoon = true;
        _ownedPerkIds.Add(perkId);
    }

    public void EnableReturnWind(string perkId)
    {
        _hasReturnWind = true;
        _ownedPerkIds.Add(perkId);
    }

    #endregion

    #region Perk Effects

    public void TriggerLingeringWind(Action<float> setEnergyCost)
    {
        if (!_hasLingeringWind) return;

        if (_lingeringWindCoroutine != null)
        {
            StopCoroutine(_lingeringWindCoroutine);
        }
        
        _lingeringWindCoroutine = StartCoroutine(LingeringWindCoroutine(_originalESkillEnergyCost, setEnergyCost));
    }

    private IEnumerator LingeringWindCoroutine(float originalCost, Action<float> setEnergyCost)
    {
        setEnergyCost(originalCost * LINGERING_WIND_DISCOUNT);
        yield return new WaitForSeconds(LINGERING_WIND_DURATION);
        setEnergyCost(originalCost);
        _lingeringWindCoroutine = null;
    }

    public void TriggerVitalWindHeal()
    {
        if (!_hasVitalWind) return;
        _healthComponent.Heal(_healthComponent.MaxHP * VITAL_WIND_HEAL_PERCENT);
    }

    public void TriggerWhirlWindHeal(int hitCount)
    {
        if (!_hasWhirlWind || hitCount <= 0) return;
        float healAmount = _healthComponent.MaxHP * WHIRL_WIND_HEAL_PERCENT * hitCount;
        _healthComponent.Heal(healAmount);
    }

    public void TriggerReturnWindEffect(float energyCost)
    {
        if (!_hasReturnWind) return;
        _energyComponent.RestoreEnergy(energyCost * RETURN_WIND_ENERGY_REFUND);
        _awakeningComponent.AddAwakening(RETURN_WIND_AWAKENING_GAIN);
    }

    public int GetTyphoonHitCount(int baseCount) => _hasTyphoon ? baseCount * 2 : baseCount;
    public float GetTyphoonInterval(float baseInterval) => _hasTyphoon ? baseInterval * 0.5f : baseInterval;

    #endregion
}
