using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierHurtAnimationBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetLayerWeight(1, 0);
        animator.SetLayerWeight(2, 0);
        animator.SetLayerWeight(3, 0);
    } 
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetLayerWeight(1, 1);
        animator.SetLayerWeight(2, 1);
        animator.SetLayerWeight(3, 1);
    }

 
}