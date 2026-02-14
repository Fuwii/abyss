using Unity.Netcode;
using UnityEngine;

namespace Game.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private NetworkObject networkObject;
        [SerializeField] private MonoBehaviour[] clientComponents;
        [SerializeField] private GameObject[] clientOnlyObjects;
        [SerializeField] private GameObject[] serverOnlyObjects;

        private void Start()
        {
            base.OnNetworkSpawn();

            if (!networkObject.IsLocalPlayer) return;

            foreach (var component in clientComponents)
            {
                component.enabled = true;
            }
        }
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }
            foreach (var obj in clientOnlyObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
            foreach (var obj in serverOnlyObjects)
            {
                if (obj != null) obj.SetActive(false);
            }

        }
    }
}