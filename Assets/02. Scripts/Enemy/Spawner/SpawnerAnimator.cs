using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SpawnerAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpawnerMovement _movement;
    private SpawnerController _controller;

    private static readonly int s_isMovingId = Animator.StringToHash("IsMoving");
    private static readonly int s_spawnId = Animator.StringToHash("Spawn");
    private static readonly int s_hurtId = Animator.StringToHash("Hurt");
    private static readonly int s_dieId = Animator.StringToHash("Die");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponentInParent<SpawnerMovement>();
        _controller = GetComponentInParent<SpawnerController>();
    }

    private void OnEnable()
    {
        _animator.Rebind();
        _animator.Update(0f);
    }

    private void Update()
    {
        if (_movement == null || _controller == null)
        {
            return;
        }

        if (_controller.CurrentState == SpawnerState.Dead)
        {
            return;
        }

        bool canMove = _controller.CurrentState == SpawnerState.Move;
        _animator.SetBool(s_isMovingId, canMove && _movement.IsMoving);
    }

    public void PlaySpawn()
    {
        _animator.ResetTrigger(s_hurtId);
        _animator.SetTrigger(s_spawnId);
    }

    public void PlayHurt()
    {
        _animator.ResetTrigger(s_spawnId);
        _animator.SetTrigger(s_hurtId);
    }

    public void PlayDie()
    {
        _animator.ResetTrigger(s_spawnId);
        _animator.ResetTrigger(s_hurtId);
        _animator.SetBool(s_isMovingId, false);
        _animator.SetTrigger(s_dieId);
    }

    public void OnSpawnHit()
    {
        _controller?.OnSpawnAnimationHit();
    }

    public void OnSpawnAnimationEnd()
    {
        _controller?.OnSpawnAnimationEnd();
    }

    public void OnHurtAnimationEnd()
    {
        _controller?.OnHurtAnimationComplete();
    }

    public void OnDeathAnimationEnd()
    {
        _controller?.OnDeathAnimationComplete();
    }
}
