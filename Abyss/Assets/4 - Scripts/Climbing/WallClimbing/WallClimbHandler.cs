using System.Collections.Generic;
using UnityEngine;

public class WallClimbHandler : IClimbHandler
{
    private readonly ClimbConfig config;
    private readonly ClimbRaycaster raycaster;
    private readonly NormalSampler normalSampler;
    private readonly ClimbMover mover;

    public WallClimbHandler(ClimbConfig cfg)
    {
        config = cfg;
        raycaster = new ClimbRaycaster();
        normalSampler = new NormalSampler();
        mover = new ClimbMover();
    }

    public bool Handle(ClimbingContext ctx)
    {
        if (ctx == null) return false;

        // stamina - поведение сохранено
        float cost = 0f;
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
        if (!raycaster.FindClimbSurface3Cast(ctx, out RaycastHit hit, out List<RaycastHit> allHits))
        {
            ctx.RequestTransitionToFalling?.Invoke();
            return false;
        }

        // discard too-flat
        if (hit.normal.y > config.MaxAllowedNormalY)
        {
            ctx.RequestTransitionToFalling?.Invoke();
            return false;
        }

        // normals sampling + smoothing
        Vector3 usedNormal = normalSampler.SampleAndSmooth(allHits, hit, ctx, config, out Vector3 lateralDir, out bool edgeDetected);

        // mover applies movement/velocity/rotation/pullup and draws debug
        mover.ApplyMovement(ctx, hit, allHits, usedNormal, lateralDir, edgeDetected, config);

        return true;
    }
}
