using System;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class ClimbingContext
    {
        public Rigidbody Rigidbody;
        public Transform Transform;
        public LayerMask ClimbableLayers;
        public float ClimbDetectionDistance;
        public float WallStickDistance;
        public float InputH;
        public float InputV;
        public bool JumpPressed;
        public float StaminaMoveCost;
        public float StaminaIdleCost;
        public float PlayerHeight = 0.7f;
        public float PullupCheckDistance = 1.5f;
        public FpsPlayerClimbing.ClimbingSubState ClimbingSubState;

        public ClimbableObject CurrentClimbable = null; // null — обычная стена
        public bool IsClimbingObject => CurrentClimbable != null;

        // delegate for consuming stamina (returns true if consumption succeeded)
        public Func<float, bool> ConsumeStamina;

        // requests hooks to the orchestrator
        public Action RequestNoStamina;
        public Action RequestTransitionToFalling;
        public Action<PullUpRequest> RequestPullUp;
    }
}