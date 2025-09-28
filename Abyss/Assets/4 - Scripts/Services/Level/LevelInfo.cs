using System;
using Core.Singleton;
using Game.Mechanics.Random.Seed;
using Unity.Netcode;

namespace Services.Level
{
    public class LevelInfo : NetworkSingleton<LevelInfo>
    {
        private readonly NetworkVariable<ulong> _seed = new();

        public ulong Seed => _seed.Value;

        protected override void Awake()
        {
            base.Awake();

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            GenerateSeed();
        }

        [Rpc(SendTo.Server, RequireOwnership = true)]
        public void SetSeedRpc(string str)
        {
            _seed.Value = str.ToSeed();
        }

        public void GenerateSeed()
        {
            SetSeedRpc(Guid.NewGuid().ToString());
        }
    }
}