using System;

namespace Game.Player.Movement.Climbing
{
    [Serializable]
    public class ClimbConfig
    {
        public float climbSpeed = 2f;
        public float descentSpeed = 6f;
        public float climbJumpForce = 5f;
        public float wallStickDistance = 0.05f;
        public float maxAllowedNormalY = 0.85f;
        public float pushTowardWallVelDeltaPerSec = 20f;
        public float pullupCheckDistance = 1.5f;
    }
}