using System.Collections.Generic;
using Core.Singleton;
using Cysharp.Threading.Tasks;
using R3;
using Services.Network.Observables;
using Unity.Netcode;
using UnityEngine;

namespace Game.Player
{
    public class PlayerSpawnController : NetworkSingleton<PlayerSpawnController>
    {
        [SerializeField] private GameObject playerAsset;

        private readonly Dictionary<ulong, NetworkObject> _players = new();
        private readonly Subject<ulong> _onPlayerSpawn = new();

        public Observable<ulong> OnPlayerSpawn => _onPlayerSpawn;
        public Observable<ulong> OnLocalPlayerSpawn => _onPlayerSpawn.Where(id => id == NetworkManager.Singleton.LocalClientId);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!NetworkManager.IsHost && !NetworkManager.IsServer)
                return;

            var token = this.GetCancellationTokenOnDestroy();
            token.Register(() => _onPlayerSpawn.Dispose());

            NetworkObservable.Instance.OnClientConnected
                .Subscribe(Spawn)
                .AddTo(token);

            NetworkObservable.Instance.OnClientDisconnect
                .Subscribe(Despawn)
                .AddTo(token);

            foreach (var client in NetworkManager.ConnectedClients.Keys)
                Spawn(client);
        }

        private void Spawn(ulong id)
        {
            var player = Instantiate(playerAsset);
            var network = player.GetComponent<NetworkObject>();

            network.SpawnAsPlayerObject(id, true);

            _players.Add(id, network);

            PlayerSpawnRpc(id);
        }

        private void Despawn(ulong id)
        {
            if (!_players.TryGetValue(id, out var network))
                return;

            if (network.IsSpawned)
                network.Despawn();

            _players.Remove(id);
        }

        [Rpc(SendTo.Everyone)]
        private void PlayerSpawnRpc(ulong id)
        {
            _onPlayerSpawn.OnNext(id);
            Debug.Log($"PlayerSpawnRpc {id}");
        }
    }
}