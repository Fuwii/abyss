public interface IRandomizer
{
    RandomCategory Category { get; }
    void Randomize(RandomizableComponent comp, ref SeededRandom rng, long worldSeed, RandomMode mode);
}
public enum RandomMode { Sequential, Independent }