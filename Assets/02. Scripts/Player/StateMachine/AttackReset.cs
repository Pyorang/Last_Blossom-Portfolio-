using UnityEngine;

public class AttackReset : StateMachineBehaviour
{
    [SerializeField] private string _triggerName;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger(_triggerName);
    }
}
