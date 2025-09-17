using System;

public interface IClimbHandler
{
    // Called every FixedUpdate

    bool Handle(ClimbingContext ctx);
}

public class ClimbConfig
{
    public float ClimbSpeed = 2f;
    public float DescentSpeed = 6f;
    public float ClimbJumpForce = 5f;
    public float WallStickDistance = 0.05f;
    public float MaxAllowedNormalY = 0.85f;
    public float PushTowardWallVelDeltaPerSec = 20f;
    public float PullupCheckDistance = 1.5f;
}

