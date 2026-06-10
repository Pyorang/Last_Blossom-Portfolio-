using UnityEngine;

public class EnemyRangedAnimator : MonoBehaviour
{
    private static readonly int s_isMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int s_attackHash = Animator.StringToHash("Attack");
    private static readonly int s_hurtHash = Animator.StringToHash("Hurt");
    private static readonly int s_dieHash = Animator.StringToHash("Die");

    private Animator _animator;
    private EnemyRangedController _controller;
    private EnemyRangedAttack _attack;
    private bool _isDead;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponentInParent<EnemyRangedController>();
        _attack = GetComponentInParent<EnemyRangedAttack>();
    }

    private void OnEnable()
    {
        _isDead = false;

        if (_animator != null)
        {
            _animator.ResetTrigger(s_attackHash);
            _animator.ResetTrigger(s_hurtHash);
            _animator.ResetTrigger(s_dieHash);
            _animator.SetBool(s_isMovingHash, false);
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (_isDead) return;
        _animator?.SetBool(s_isMovingHash, isMoving);
    }

    public void PlayTrigger(string triggerName)
    {
        if (_isDead) return;
        _animator?.SetTrigger(triggerName);
    }

    public void PlayHurt()
    {
        if (_isDead) return;
        _animator?.SetTrigger(s_hurtHash);
    }

    public void PlayDie()
    {
        if (_animator == null) return;

        _isDead = true;
        _animator.ResetTrigger(s_attackHash);
        _animator.ResetTrigger(s_hurtHash);
        _animator.SetTrigger(s_dieHash);
    }

    public void OnFireProjectile()
    {
        _attack?.OnFireProjectile();
    }

    public void OnAttackEnd()
    {
        _attack?.OnAttackAnimationEnd();
    }

    public void OnHitEnd()
    {
        _controller?.OnHitAnimationComplete();
    }

    public void OnDeathEnd()
    {
        _controller?.OnDeathAnimationComplete();
    }
}
