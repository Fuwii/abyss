using Eflatun.SceneReference;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Locations.Lobby
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private SceneReference gameScene;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter) && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
                NetworkManager.Singleton.SceneManager.LoadScene(gameScene.Name, LoadSceneMode.Single);
            }
        }
    }
}