using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KamikazeAnimator : MonoBehaviour
{
    private Animator _animator;
    private KamikazeMovement _movement;
    private KamikazeController _controller;

    private static readonly int s_isMovingId = Animator.StringToHash("IsMoving");
    private static readonly int s_detonateId = Animator.StringToHash("Detonate");
    private static readonly int s_dieId = Animator.StringToHash("Die");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponentInParent<KamikazeMovement>();
        _controller = GetComponentInParent<KamikazeController>();
    }

    private void OnEnable()
    {
        _animator.Rebind();
        _animator.Update(0f);
    }

    private void Update()
    {
        if (_movement == null)
        {
            return;
        }

        bool canMove = _controller == null ||
                       (_controller.CurrentState != KamikazeState.Detonating &&
                        _controller.CurrentState != KamikazeState.Dead);

        _animator.SetBool(s_isMovingId, canMove && _movement.IsMoving);
    }

    public void PlayDetonate()
    {
        _animator.SetTrigger(s_detonateId);
    }

    public void PlayDie()
    {
        _animator.SetTrigger(s_dieId);
    }

    public void OnDeathAnimationEnd()
    {
        _controller?.OnDeathAnimationComplete();
    }
}
