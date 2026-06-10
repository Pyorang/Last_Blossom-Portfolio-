using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private EnemyMovement _movement;
    [SerializeField] private EnemyAttack _attack;
    [SerializeField] private EnemyController _controller;

    private Animator _animator;
    private static readonly int s_isMovingId = Animator.StringToHash("IsMoving");
    private static readonly int s_hurtId = Animator.StringToHash("Hurt");
    private static readonly int s_dieId = Animator.StringToHash("Die");

    public Animator Animator => _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        if (_movement == null)
        {
            _movement = GetComponentInParent<EnemyMovement>();
        }
        if (_attack == null)
        {
            _attack = GetComponentInParent<EnemyAttack>();
        }
        if (_controller == null)
        {
            _controller = GetComponentInParent<EnemyController>();
        }
    }

    private void OnEnable()
    {
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }
    }

    private void Update()
    {
        if (_movement == null)
        {
            return;
        }
        
        bool canMove = _controller == null || 
                       (_controller.CurrentState != EnemyState.Hit && 
                        _controller.CurrentState != EnemyState.Dead);
        
        _animator.SetBool(s_isMovingId, canMove && _movement.IsMoving);
    }

    public void PlayTrigger(string triggerName)
    {
        _animator.SetTrigger(triggerName);
    }

    public void PlayHurt()
    {
        _animator.SetTrigger(s_hurtId);
    }

    public void PlayDie()
    {
        _animator.SetTrigger(s_dieId);
    }

    public void OnAttackHit()
    {
        _attack?.OnAttackHitFrame();
    }
    
    public void OnAttackHitLeft()
    {
        _attack?.OnAttackHitLeft();
    }
    
    public void OnAttackHitRight()
    {
        _attack?.OnAttackHitRight();
    }
    
    public void OnAttackHitBoth()
    {
        _attack?.OnAttackHitBoth();
    }
    
    public void OnAttackHitEnd()
    {
        _attack?.OnAttackHitEnd();
    }

    public void OnAttackEnd()
    {
        _attack?.OnAttackEndFrame();
    }

    public void OnHitAnimationEnd()
    {
        _controller?.OnHitAnimationComplete();
    }

    public void OnDeathAnimationEnd()
    {
        _controller?.OnDeathAnimationComplete();
    }
}
