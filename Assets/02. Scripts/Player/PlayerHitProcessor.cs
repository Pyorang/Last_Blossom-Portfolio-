using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerCombatStats))]
[RequireComponent(typeof(PlayerPerkHandler))]
[RequireComponent(typeof(EnergyComponent))]
[RequireComponent(typeof(AwakeningComponent))]
[RequireComponent(typeof(PlayerVFX))]
[RequireComponent(typeof(PlayerSFX))]
public class PlayerHitProcessor : MonoBehaviour
{
    private PlayerCombatStats _combatStats;
    private PlayerPerkHandler _perkHandler;
    private EnergyComponent _energyComponent;
    private AwakeningComponent _awakeningComponent;
    private PlayerVFX _playerVFX;
    private PlayerSFX _playerSFX;

    private HashSet<int> _hitSFXPlayedEnemies = new();

    private void Awake()
    {
        _combatStats = GetComponent<PlayerCombatStats>();
        _perkHandler = GetComponent<PlayerPerkHandler>();
        _energyComponent = GetComponent<EnergyComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
        _playerVFX = GetComponent<PlayerVFX>();
        _playerSFX = GetComponent<PlayerSFX>();
    }

    public void ProcessHits(Collider[] hits, int count, float coefficient, bool isEnhanced, PlayerHitType hitType)
    {
        bool hitAny = false;
        bool playedHitStop = false;

        for (int i = 0; i < count; i++)
        {
            if (!hits[i].TryGetComponent<IDamageable>(out var damageable)) continue;

            float damage = _combatStats.CalculateDamage(coefficient, isEnhanced, out bool isCritical);
            damageable.TakeDamage(damage, isEnhanced, isCritical);

            _playerVFX.PlayHitVFX(hits[i].transform.position, isEnhanced);

            _energyComponent.RestoreEnergy(_combatStats.HitEnergyRecovery);
            _awakeningComponent.OnHit();

            if (_perkHandler.HasVitalWind)
            {
                _perkHandler.TriggerVitalWindHeal();
            }

            int enemyId = hits[i].GetInstanceID();
            if (!_hitSFXPlayedEnemies.Contains(enemyId))
            {
                _hitSFXPlayedEnemies.Add(enemyId);
                _playerSFX.PlayHitSFX(hitType);
            }

            hitAny = true;
        }

        if (hitAny && !playedHitStop)
        {
            if (isEnhanced)
            {
                TimeScaleManager.Instance?.PlayMediumHitStop();
            }
            else
            {
                TimeScaleManager.Instance?.PlayLightHitStop();
            }
            CameraController.Instance?.Shake();
            playedHitStop = true;
        }
    }

    public void ResetHitSFXTracking()
    {
        _hitSFXPlayedEnemies.Clear();
    }
}
