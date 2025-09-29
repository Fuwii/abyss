using Game.Player.Movement.Climbing;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public FpsPlayerClimbing climbing;
    public Animator animator;


    void Update()
    {
        if (climbing == null) return;

        animator.SetBool("IsClimbing", climbing.state == FpsPlayerClimbing.PlayerState.Climbing);
        animator.SetBool("IsWalking", climbing.state == FpsPlayerClimbing.PlayerState.Walking);
        animator.SetFloat("InputH", climbing.CurrentInputMove.x);
        animator.SetFloat("InputV", climbing.CurrentInputMove.y);
    }
}
