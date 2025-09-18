using UnityEngine;
using UnityEngine.UI;

namespace Game.Locations.Menu
{
    public class MenuCanvas : MonoBehaviour
    {
        [SerializeField] private Button singleplayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button exitButton;

        private void Start()
        {
            var manager = MenuManager.Instance;

            singleplayerButton.onClick.AddListener(manager.Singleplayer);
            multiplayerButton.onClick.AddListener(manager.Multiplayer);
            exitButton.onClick.AddListener(manager.Exit);
        }
    }
}