using Game.Player.Movement.Climbing;
using UnityEngine;
using Unity.Netcode; 

public class PlayerAnimationController : NetworkBehaviour
{
    public FpsPlayerClimbing climbing;
    public Animator animator;

    void Update()
    {
        if (!IsOwner) return;

        if (climbing == null) return;

        animator.SetBool("IsClimbing", climbing.state == FpsPlayerClimbing.PlayerState.Climbing);
        animator.SetBool("IsWalking", climbing.state == FpsPlayerClimbing.PlayerState.Walking);
        animator.SetFloat("InputH", climbing.CurrentInputMove.x);
        animator.SetFloat("InputV", climbing.CurrentInputMove.y);
    }
}