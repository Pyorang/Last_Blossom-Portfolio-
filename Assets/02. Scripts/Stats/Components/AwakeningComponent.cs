using System;
using UnityEngine;

public class AwakeningComponent : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private VFXGroup _awakeningVFX;
    
    private float _maxAwakening;
    private float _hitAwakeningRecovery;
    private float _awakeningDuration;

    private float _currentAwakening;
    private bool _isAwakened;
    private float _awakeningTimer;
    private float _uiUpdateTimer;

    public float CurrentAwakening => _currentAwakening;
    public float MaxAwakening => _maxAwakening;
    public float AwakeningRatio => _maxAwakening > 0 ? _currentAwakening / _maxAwakening : 0f;
    public bool IsAwakened => _isAwakened;
    public float HitAwakeningRecovery => _hitAwakeningRecovery;

    public event Action<float, float> OnAwakeningChanged;
    public event Action OnAwakeningActivated;
    public event Action OnAwakeningDeactivated;

    private void Update()
    {
        if (!_isAwakened) return;
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying) return;

        _awakeningTimer -= Time.deltaTime;
        _uiUpdateTimer += Time.deltaTime;

        if (_uiUpdateTimer >= 1f)
        {
            _uiUpdateTimer = 0f;
            OnAwakeningChanged?.Invoke(_awakeningTimer, _awakeningDuration);
        }

        if (_awakeningTimer <= 0f)
        {
            DeactivateAwakening();
        }
    }

    public void Initialize(float max, float hitRecovery, float duration)
    {
        _maxAwakening = max;
        _hitAwakeningRecovery = hitRecovery;
        _awakeningDuration = duration;
        _currentAwakening = 0f;
        _isAwakened = false;

        OnAwakeningChanged?.Invoke(_currentAwakening, _maxAwakening);
    }

    public void AddAwakening(float amount)
    {
        if (_isAwakened || amount <= 0) return;

        float previous = _currentAwakening;
        _currentAwakening = Mathf.Min(_currentAwakening + amount, _maxAwakening);

        if (_currentAwakening > previous)
        {
            OnAwakeningChanged?.Invoke(_currentAwakening, _maxAwakening);
        }
    }

    public bool TryActivateAwakening()
    {
        if (!CanActivate()) return false;

        _isAwakened = true;
        _awakeningTimer = _awakeningDuration;
        _uiUpdateTimer = 0f;
        _currentAwakening = 0f;

        CameraController.Instance?.StrongShake();

        OnAwakeningActivated?.Invoke();
        OnAwakeningChanged?.Invoke(_awakeningTimer, _awakeningDuration);

        return true;
    }

    public bool CanActivate()
    {
        if (_isAwakened) return false;
        if (_currentAwakening < _maxAwakening) return false;
        return true;
    }

    private void DeactivateAwakening()
    {
        _isAwakened = false;
            
        OnAwakeningDeactivated?.Invoke();
        OnAwakeningChanged?.Invoke(_currentAwakening, _maxAwakening);
    }

    public void OnHit()
    {
        AddAwakening(_hitAwakeningRecovery);
    }

    public void PlayAwakeningVFX()
    {
        _awakeningVFX?.PlayAll();
    }

    #region Perk Methods

    public void AddHitAwakeningRecoveryPercent(float percent)
    {
        _hitAwakeningRecovery *= (1f + percent);
    }

    public void AddAwakeningDuration(float seconds)
    {
        _awakeningDuration += seconds;
    }

    #endregion
}
