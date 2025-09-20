using Game.Player.Movement.Climbing.WallClimbing;

namespace Game.Player.Movement.Climbing
{
    public class ClimbHandlerFactory
    {
        private readonly WallClimbHandler _wallHandler;
        private readonly ObjectClimbHandler _objectHandler;
        private ClimbConfig _climbConfig;

        public ClimbHandlerFactory(WallClimbHandler wallHandler, ObjectClimbHandler objectHandler)
        {
            _wallHandler = wallHandler;
            _objectHandler = objectHandler;
        }

        public IClimbHandler GetHandler(ClimbingContext ctx)
        {
            return ctx.IsClimbingObject ? _objectHandler : _wallHandler;
        }
    }
}