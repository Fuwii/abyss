using Services.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Locations.Menu
{
    public class MenuDebugCanvas : MonoBehaviour
    {
        [SerializeField] private Button joinButton;

        private void Awake()
        {
#if !DEBUG
            Destroy(gameObject);
#endif
        }

        private void Start()
        {
            joinButton.onClick.AddListener(Join);
        }

        private void Join()
        {
            NetworkService.Instance.Join();
        }
    }
}