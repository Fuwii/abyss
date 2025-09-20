using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace Core.Singleton
{
    public abstract class NetworkService<T> : MonoBehaviour
        where T : NetworkService<T>
    {
        public static T Instance { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        public CancellationToken ServiceCancellationToken => _cancellationTokenSource.Token;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"Another network service of {Instance} is already exists");
                Destroy(this);
                return;
            }

            NetworkManager.OnInstantiated += OnNetworkPreSpawn;
            NetworkManager.OnDestroying += OnNetworkPreDespawn;

            _cancellationTokenSource = new CancellationTokenSource();
            Instance = this as T;
        }

        private void OnDestroy()
        {
            NetworkManager.OnInstantiated -= OnNetworkPreSpawn;
            NetworkManager.OnDestroying -= OnNetworkPreDespawn;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        protected virtual void OnNetworkPreSpawn(NetworkManager manager) { }

        protected virtual void OnNetworkPreDespawn(NetworkManager manager)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}