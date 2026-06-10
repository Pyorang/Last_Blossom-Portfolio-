using System;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _isPlayer = false;

    private float _maxHP = 100f;
    private float _hpRegenPerSecond = 0f;
    private float _regenDelay = 1f;
    private float _currentHP;
    private float _regenDelayTimer;
    private float _regenTimer;
    private bool _isDead = false;
    public bool _isInvincible = false;
    private bool _showDamageText = true;
    
    public float CurrentHP => _currentHP;
    public float MaxHP => _maxHP;
    public float HPRegenPerSecond => _hpRegenPerSecond;
    public bool IsDead => _isDead;
    public bool IsInvincible => _isInvincible;
    public float HPRatio => _maxHP > 0 ? _currentHP / _maxHP : 0f;

    // 데미지 정보를 담는 클래스 (참조 타입이라 수정 가능)
    public class DamageInfo
    {
        public float damage;
        public bool isEnhanced;
        public bool isCritical;
        public bool isShielded;
    }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action<float> OnDamageTaken;
    public event Action OnHealed;
    public event Action<DamageInfo> OnBeforeDamage;  // 데미지 적용 전 이벤트
    
    private void Update()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying) return;

        if (_regenDelayTimer > 0)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        if (_hpRegenPerSecond > 0 && _currentHP < _maxHP)
        {
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 1f)
            {
                _regenTimer -= 1f;
                Heal(_hpRegenPerSecond);
            }
        }
    }
    
    public void Initialize(float max, float regenPerSecond = 0f, float regenDelay = 1f)
    {
        _maxHP = max;
        _hpRegenPerSecond = regenPerSecond;
        _regenDelay = regenDelay;
        _currentHP = _maxHP;
        _regenDelayTimer = 0f;
        _regenTimer = 0f;
        _isDead = false;
        _isInvincible = false;
        
        OnHealthChanged?.Invoke(_currentHP, _maxHP);
    }
    
    public void TakeDamage(float damage, bool isEnhanced = false, bool isCritical = false)
    {
        if (_isDead || _isInvincible || damage <= 0)
        {
            return;
        }
        
        // 데미지 정보 생성 및 OnBeforeDamage 이벤트 발생
        var damageInfo = new DamageInfo
        {
            damage = damage,
            isEnhanced = isEnhanced,
            isCritical = isCritical,
            isShielded = false
        };
        OnBeforeDamage?.Invoke(damageInfo);
        
        // 수정된 데미지 적용
        float actualDamage = Mathf.Min(_currentHP, damageInfo.damage);
        _currentHP -= actualDamage;
        _regenDelayTimer = _regenDelay;
        
        if (_showDamageText && gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            DamageTextPool.Instance?.Spawn(
                transform.position, 
                Mathf.RoundToInt(actualDamage), 
                damageInfo.isEnhanced, 
                damageInfo.isCritical, 
                damageInfo.isShielded
            );
        }
        
        OnHealthChanged?.Invoke(_currentHP, _maxHP);
        
        if (_currentHP <= 0)
        {
            Die();
        }
        else
        {
            OnDamageTaken?.Invoke(actualDamage);
        }
    }
    
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0)
        {
            return;
        }
        
        float actualHeal = Mathf.Min(_maxHP - _currentHP, amount);
        if (actualHeal <= 0)
        {
            return;
        }
        
        _currentHP += actualHeal;
        
        OnHealed?.Invoke();
        OnHealthChanged?.Invoke(_currentHP, _maxHP);
    }
    
    public void ModifyMaxHP(float newMaxHP)
    {
        _maxHP = Mathf.Max(1f, newMaxHP);
        _currentHP = Mathf.Min(_currentHP, _maxHP);
        
        OnHealthChanged?.Invoke(_currentHP, _maxHP);
    }

    public void SetHPRegen(float regenPerSecond)
    {
        _hpRegenPerSecond = Mathf.Max(0f, regenPerSecond);
    }

    public void SetRegenDelay(float delay)
    {
        _regenDelay = Mathf.Max(0f, delay);
    }

    public void SetInvincibleOn()
    {
        _isInvincible = true;
    }

    public void SetInvincibleOff()
    {
        _isInvincible = false;
    }

    public void SetShowDamageText(bool show)
    {
        _showDamageText = show;
    }

    public void Kill()
    {
        _currentHP = 0;
        Die();
    }
    
    public void Revive(float hpPercent = 1f)
    {
        if (!_isDead)
        {
            return;
        }

        _isDead = false;
        _currentHP = _maxHP * Mathf.Clamp01(hpPercent);
        _regenDelayTimer = 0f;
        
        OnHealthChanged?.Invoke(_currentHP, _maxHP);
    }
    
    private void Die()
    {
        if (_isDead)
        {
            return;
        }
        
        _isDead = true;
        
        if (_isPlayer)
        {
            GameStateManager.Instance?.EndGame(false);
        }
        
        OnDeath?.Invoke();
    }

    private void OnDestroy()
    {
        OnHealthChanged = null;
        OnDeath = null;
        OnDamageTaken = null;
        OnHealed = null;
        OnBeforeDamage = null;
    }
}
