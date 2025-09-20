using System;
using Core.Singleton;
using Eflatun.SceneReference;
using Services.Network;
using Services.Network.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Locations.Menu
{
    public class MenuManager : Singleton<MenuManager>
    {
        [SerializeField] private SceneReference lobbyScene;

        [SerializeField] private LobbyOptions singleplayerLobbyOptions;
        [SerializeField] private LobbyOptions multiplayerLobbyOptions;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += LoadLobbyAfterHost;
        }

        public async void Singleplayer()
        {
            try
            {
                await SteamService.Instance.CreateLobby(singleplayerLobbyOptions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void Multiplayer()
        {
            try
            {
                await SteamService.Instance.CreateLobby(multiplayerLobbyOptions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        private void LoadLobbyAfterHost()
        {
            NetworkManager.Singleton.OnServerStarted -= LoadLobbyAfterHost;

            NetworkManager.Singleton.SceneManager.LoadScene(lobbyScene.Path, LoadSceneMode.Single);
        }
    }
}