using System;
using UnityEngine;

public class EnergyComponent : MonoBehaviour
{
    private float _maxEnergy = 100f;
    private float _energyRegenPerSecond = 5f;
    private float _regenDelay = 1f;
    private float _currentEnergy;
    private float _regenDelayTimer;
    private float _regenTimer;

    public float CurrentEnergy => _currentEnergy;
    public float MaxEnergy => _maxEnergy;
    public float EnergyRegenPerSecond => _energyRegenPerSecond;
    public float EnergyRatio => _maxEnergy > 0 ? _currentEnergy / _maxEnergy : 0f;

    public event Action<float, float> OnEnergyChanged;
    public event Action OnEnergyUsed;

    private void Update()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying) return;

        if (_regenDelayTimer > 0)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        if (_energyRegenPerSecond > 0 && _currentEnergy < _maxEnergy)
        {
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 1f)
            {
                _regenTimer -= 1f;
                RestoreEnergy(_energyRegenPerSecond);
            }
        }
    }

    public void Initialize(float max, float regenPerSecond, float regenDelay = 1f)
    {
        _maxEnergy = max;
        _energyRegenPerSecond = regenPerSecond;
        _regenDelay = regenDelay;
        _currentEnergy = _maxEnergy;
        _regenDelayTimer = 0f;
        _regenTimer = 0f;

        OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (amount <= 0 || _currentEnergy < amount) return false;

        _currentEnergy -= amount;
        _regenDelayTimer = _regenDelay;

        OnEnergyUsed?.Invoke();
        OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);

        return true;
    }

    public bool HasEnoughEnergy(float amount)
    {
        return _currentEnergy >= amount;
    }

    public void RestoreEnergy(float amount)
    {
        if (amount <= 0) return;

        float previousEnergy = _currentEnergy;
        _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);

        if (_currentEnergy > previousEnergy)
        {
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
        }
    }

    public void ModifyMaxEnergy(float newMaxEnergy)
    {
        _maxEnergy = Mathf.Max(1f, newMaxEnergy);
        _currentEnergy = Mathf.Min(_currentEnergy, _maxEnergy);

        OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
    }

    public void SetEnergyRegen(float regenPerSecond)
    {
        _energyRegenPerSecond = Mathf.Max(0f, regenPerSecond);
    }

    public void SetRegenDelay(float delay)
    {
        _regenDelay = Mathf.Max(0f, delay);
    }
}
