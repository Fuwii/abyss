using Game.Mechanics.Random.Seed;

namespace Game.Mechanics.Random
{
    public interface IRandomizer
    {
        RandomCategory Category { get; }
        void Randomize(RandomizableComponent comp, ref SeededRandom rng, long worldSeed, RandomMode mode);
    }
}