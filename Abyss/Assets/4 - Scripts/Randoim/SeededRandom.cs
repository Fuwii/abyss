
using System.Collections.Generic;
// not sure in needable or not
public struct SeededRandom
{
    private ulong state;
    public SeededRandom(ulong seed) { state = seed; state = SplitMix64(state); state = SplitMix64(state); }

    private static ulong SplitMix64(ulong x) { x += 0x9E3779B97F4A7C15UL; x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL; x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL; return x ^ (x >> 31); }

    private ulong NextRaw()
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z = z ^ (z >> 31);
        return z;
    }

    public double NextDouble() { const ulong mask = (1UL << 53) - 1; return (NextRaw() & mask) / (double)(1UL << 53); }
    public int NextInt(int minInclusive, int maxExclusive) { if (minInclusive >= maxExclusive) return minInclusive; return minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive)); }
    public List<int> ShuffleIndices(int count) { var arr = new List<int>(count); for (int i = 0; i < count; i++) arr.Add(i); for (int i = count - 1; i > 0; i--) { int j = NextInt(0, i + 1); int t = arr[i]; arr[i] = arr[j]; arr[j] = t; } return arr; }
}
