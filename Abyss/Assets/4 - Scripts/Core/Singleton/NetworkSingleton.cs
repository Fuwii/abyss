using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace Core.Singleton
{
    public class NetworkSingleton<T> : NetworkBehaviour
        where T : NetworkSingleton<T>
    {
        public static T Instance { get; private set; }

        public CancellationToken NetworkCancellationToken => _tokenSource.Token;

        private CancellationTokenSource _tokenSource;

        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            base.OnNetworkPreSpawn(ref networkManager);

            if (Instance != null)
            {
                Debug.LogWarning($"Another instance of {Instance} is already exists");
                Destroy(this);
            }

            _tokenSource = new CancellationTokenSource();
            Instance = this as T;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance != this)
                return;

            _tokenSource.Cancel();
            _tokenSource.Dispose();

            Instance = null;

            base.OnNetworkDespawn();
        }
    }
}