using Core.Singleton;
using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Services.Network
{
    public sealed class NetworkService : Service<NetworkService>
    {
        private readonly CompositeDisposable _disposable = new();

        private void Start()
        {
            var steam = SteamService.Instance;

            steam.OnLobbyCreated
                .Subscribe(_ => Host())
                .AddTo(_disposable);

            steam.OnLobbyEntered
                .Subscribe(_ => Join())
                .AddTo(_disposable);
        }

        protected override void OnDestroy()
        {
            _disposable.Dispose();

            NetworkManager.Singleton?.Shutdown();

            base.OnDestroy();
        }

        public void Host()
        {
            if (NetworkManager.Singleton.IsHost) return;

            SetupTransport();

            NetworkManager.Singleton.StartHost();
        }

        public void Join()
        {
            if (NetworkManager.Singleton.IsHost) return;
            if (NetworkManager.Singleton.IsClient) return;

            SetupTransport();

            NetworkManager.Singleton.StartClient();
        }

        private void SetupTransport()
        {
#if UNITY_EDITOR
            var transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 8888);

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
#else
            var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
            transport.targetSteamId = lobby.Owner.Id;
#endif
        }
    }
}