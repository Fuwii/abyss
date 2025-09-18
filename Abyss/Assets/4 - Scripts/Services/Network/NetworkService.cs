using Core;
using Netcode.Transports.Facepunch;
using Steamworks.Data;
using R3;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace Services.Network
{
    public class NetworkService : Service<NetworkService>
    {
        private readonly CompositeDisposable _disposable = new();

        private void Start()
        {
            var steam = SteamService.Instance;

            steam.OnLobbyCreated
                .Subscribe(Host)
                .AddTo(_disposable);

            steam.OnLobbyEntered
                .Subscribe(Join)
                .AddTo(_disposable);
        }

        public void Host(Lobby lobby)
        {
            if (SteamClient.SteamId != lobby.Owner.Id) return;
            if (NetworkManager.Singleton.IsHost) return;

            var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
            transport.targetSteamId = lobby.Owner.Id;

            NetworkManager.Singleton.StartHost();
        }

        public void Join(Lobby lobby)
        {
            if (SteamClient.SteamId == lobby.Owner.Id) return;
            if (NetworkManager.Singleton.IsHost) return;
            if (NetworkManager.Singleton.IsClient) return;

            var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
            transport.targetSteamId = lobby.Owner.Id;

            NetworkManager.Singleton.StartClient();
        }

        private void OnDestroy()
        {
            _disposable.Dispose();

            NetworkManager.Singleton?.Shutdown();
        }
    }
}