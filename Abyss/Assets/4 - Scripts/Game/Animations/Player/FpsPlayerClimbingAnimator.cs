using Game.Player.Movement.Climbing;
using UnityEngine;

public class FpsPlayerClimbingAnimator : MonoBehaviour
{
    public FpsPlayerClimbing climbing;
    public Animator animator;


    void Update()
    {
        if (climbing == null) return;

        animator.SetBool("IsClimbing", climbing.state == FpsPlayerClimbing.PlayerState.Climbing);

        animator.SetFloat("InputH", climbing.CurrentInputMove.x);
        animator.SetFloat("InputV", climbing.CurrentInputMove.y);
    }
}
