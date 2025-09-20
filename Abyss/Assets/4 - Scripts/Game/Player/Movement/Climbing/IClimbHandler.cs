namespace Game.Player.Movement.Climbing
{
    public interface IClimbHandler
    {
        // Called every FixedUpdate

        bool Handle(ClimbingContext ctx);
    }
}