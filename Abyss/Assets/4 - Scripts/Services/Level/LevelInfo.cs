using Core.Singleton;
using Unity.Netcode;

namespace Services.Level
{
    public class LevelInfo : NetworkService<LevelInfo>
    {
        private ulong _seed;

        public ulong Seed => _seed;

        [Rpc(SendTo.Server)]
        public void SetSeed(ulong seed)
        {
            _seed = seed;
        }
    }
}