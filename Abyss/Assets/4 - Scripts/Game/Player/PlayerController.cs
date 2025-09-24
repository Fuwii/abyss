using Unity.Netcode;
using UnityEngine;

namespace Game.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private NetworkObject networkObject;
        [SerializeField] private MonoBehaviour[] clientComponents;

        private void Start()
        {
            base.OnNetworkSpawn();
            
            if (!networkObject.IsLocalPlayer) return;

            foreach (var component in clientComponents)
            {
                component.enabled = true;
            }
        }
    }
}