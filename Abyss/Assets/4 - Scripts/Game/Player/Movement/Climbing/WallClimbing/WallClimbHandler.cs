using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class WallClimbHandler : IClimbHandler
    {
        private readonly ClimbConfig _config;
        private readonly ClimbRaycaster _raycaster;
        private readonly NormalSampler _normalSampler;
        private readonly ClimbMover _mover;

        public WallClimbHandler(ClimbConfig cfg)
        {
            _config = cfg;
            _raycaster = new ClimbRaycaster();
            _normalSampler = new NormalSampler();
            _mover = new ClimbMover();
        }

        public bool Handle(ClimbingContext ctx)
        {
            if (ctx == null) return false;

            // stamina - поведение сохранено
            var cost = 0f;
            switch (ctx.ClimbingSubState)
            {
                case FpsPlayerClimbing.ClimbingSubState.Moving:
                    cost = ctx.StaminaMoveCost * Time.fixedDeltaTime;
                    break;
                case FpsPlayerClimbing.ClimbingSubState.Idle:
                    cost = ctx.StaminaIdleCost * Time.fixedDeltaTime;
                    break;
                case FpsPlayerClimbing.ClimbingSubState.Hanging:
                    cost = 0f;
                    break;
            }

            if (cost > 0f && ctx.ConsumeStamina != null && !ctx.ConsumeStamina(cost))
            {
                ctx.RequestNoStamina();
                return false;
            }

            // detection
            if (!_raycaster.FindClimbSurface3Cast(ctx, out var hit, out var allHits))
            {
                ctx.RequestTransitionToFalling?.Invoke();
                return false;
            }

            // discard too-flat
            if (hit.normal.y > _config.maxAllowedNormalY)
            {
                ctx.RequestTransitionToFalling?.Invoke();
                return false;
            }

            // normals sampling + smoothing
            var usedNormal = _normalSampler.SampleAndSmooth(allHits, hit, ctx, _config, out var lateralDir, out var edgeDetected);

            // mover applies movement/velocity/rotation/pullup and draws debug
            _mover.ApplyMovement(ctx, hit, allHits, usedNormal, lateralDir, edgeDetected, _config);

            return true;
        }
    }
}