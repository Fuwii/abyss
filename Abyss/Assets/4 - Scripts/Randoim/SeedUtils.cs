// SeedUtils
public static class SeedUtils
{
    public static ulong CombineSeed(long worldSeed, long id, string role)
    {
        unchecked
        {
            ulong h = (ulong)worldSeed;
            h = h * 0x9ddfea08eb382d69UL + ((ulong)id + 0x9e3779b97f4a7c15UL);
            ulong r = 1469598103934665603UL;
            foreach (char c in role) r = (r ^ (byte)c) * 1099511628211UL;
            h ^= r + 0x9e3779b97f4a7c15UL;
            return h;
        }
    }
}
