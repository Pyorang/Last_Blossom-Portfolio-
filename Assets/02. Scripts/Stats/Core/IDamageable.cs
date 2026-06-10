using System;

public interface IDamageable
{
    float CurrentHP { get; }
    
    float MaxHP { get; }

    bool IsDead { get; }
    
    void TakeDamage(float damage, bool isEnhanced = false, bool isCritical = false);
    
    void Heal(float amount);
    
    event Action<float, float> OnHealthChanged;

    event Action OnDeath;
}
