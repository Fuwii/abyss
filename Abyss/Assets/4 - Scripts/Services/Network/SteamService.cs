using Core.Singleton;
using Cysharp.Threading.Tasks;
using R3;
using Services.Network.Data;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Result = Steamworks.Result;

namespace Services.Network
{
    [DisallowMultipleComponent]
    public class SteamService : Service<SteamService>
    {
        [SerializeField] private uint appID = 480;

        private Lobby _lobby;
        private LobbyOptions _options;

        private readonly Subject<Lobby> _onLobbyCreated = new();
        private readonly Subject<Lobby> _onLobbyEntered = new();

        public Lobby Lobby => _lobby;

        public Observable<Lobby> OnLobbyCreated => _onLobbyCreated;
        public Observable<Lobby> OnLobbyEntered => _onLobbyEntered;

        private void Start()
        {
            if (!SteamClient.IsValid) SteamClient.Init(appID);

            SteamMatchmaking.OnLobbyCreated += LobbyCreated;
            SteamMatchmaking.OnLobbyEntered += LobbyEntered;

            SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberDisconnected += LobbyMemberDisconnected;

            SteamFriends.OnGameLobbyJoinRequested += LobbyJoinRequested;
        }

        protected override void OnDestroy()
        {
            _onLobbyCreated.Dispose();
            _onLobbyEntered.Dispose();

            SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= LobbyEntered;

            SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberDisconnected -= LobbyMemberDisconnected;

            SteamFriends.OnGameLobbyJoinRequested -= LobbyJoinRequested;

            if (SteamClient.IsValid) SteamClient.Shutdown();

            base.OnDestroy();
        }

        public async UniTask CreateLobby(LobbyOptions options)
        {
            _options = options;

            await SteamMatchmaking.CreateLobbyAsync(options.MaxPlayers);
        }

        #region Lobby

        private void LobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                Debug.LogError($"Lobby creation failed: {result}");
                return;
            }

            ApplyOptions(lobby, _options);

            _lobby = lobby;
            _onLobbyCreated.OnNext(lobby);

            Debug.Log($"Lobby created: {result}");
        }

        private void LobbyEntered(Lobby lobby)
        {
            _onLobbyEntered.OnNext(lobby);

            Debug.Log($"Entered lobby {lobby}");
        }

        private void LobbyJoinRequested(Lobby lobby, SteamId steamId)
        {
            if (_lobby.Id == lobby.Id)
            {
                Debug.Log($"Player already in lobby {lobby}");
                return;
            }

            lobby.Join();
        }

        #endregion

        #region Member

        private void LobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{lobby}, {friend}");
        }

        private void LobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{lobby}, {friend}");
        }

        #endregion

        private static void ApplyOptions(Lobby lobby, LobbyOptions options)
        {
            switch (options.LobbyType)
            {
                case LobbyType.FriendsOnly:
                    lobby.SetFriendsOnly();
                    break;
                case LobbyType.Public:
                    lobby.SetPublic();
                    break;
                case LobbyType.Private:
                default:
                    lobby.SetPrivate();
                    break;
            }
        }
    }
}