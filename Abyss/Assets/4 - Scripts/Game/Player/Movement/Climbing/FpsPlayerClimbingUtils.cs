using System;
using Game.Player.Input;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public static class FpsPlayerClimbingUtils
    {
        public static void SubscribeInput(Action<Vector2> onMove, Action<Vector2> onLook, Action onJump, Action onLeftStart, Action onLeftCancel)
        {
            InputManager.OnMoveAxisChanged += onMove;
            InputManager.OnLookDeltaChanged += onLook;
            InputManager.OnJumpPressed += onJump;
            InputManager.OnLeftClickStarted += onLeftStart;
            InputManager.OnLeftClickCanceled += onLeftCancel;
        }

        public static void UnsubscribeInput(Action<Vector2> onMove, Action<Vector2> onLook, Action onJump, Action onLeftStart, Action onLeftCancel)
        {
            InputManager.OnMoveAxisChanged -= onMove;
            InputManager.OnLookDeltaChanged -= onLook;
            InputManager.OnJumpPressed -= onJump;
            InputManager.OnLeftClickStarted -= onLeftStart;
            InputManager.OnLeftClickCanceled -= onLeftCancel;
        }

        public static ClimbConfig BuildClimbConfig(float wallStickDistance, float climbSpeed, float descentSpeed) =>
            new ClimbConfig { wallStickDistance = wallStickDistance, climbSpeed = climbSpeed, descentSpeed = descentSpeed };

        public static void UpdateClimbingContext(
            ClimbingContext ctx,
            Rigidbody rb,
            Transform transform,
            LayerMask climbableLayers,
            float wallStickDistance,
            float climbDetectionDistance,
            Vector2 moveInput,
            bool jumpPressed,
            float staminaMoveCost,
            float staminaIdleCost,
            FpsPlayerClimbing.ClimbingSubState climbingState,
            Func<bool> isGrounded,
            Func<float, bool> consumeStamina,
            Action requestNoStamina)
        {
            if (ctx == null)
                return;

            ctx.Rigidbody = rb;
            ctx.Transform = transform;
            ctx.ClimbableLayers = climbableLayers;
            ctx.WallStickDistance = wallStickDistance;
            ctx.ClimbDetectionDistance = climbDetectionDistance;
            ctx.MoveInput = moveInput;
            ctx.JumpPressed = jumpPressed;
            ctx.StaminaMoveCost = staminaMoveCost;
            ctx.StaminaIdleCost = staminaIdleCost;
            ctx.ClimbingSubState = climbingState;
            ctx.IsGrounded = isGrounded;
            ctx.ConsumeStamina = consumeStamina;
            ctx.RequestNoStamina = requestNoStamina;
        }
    }
}