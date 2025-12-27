using UnityEngine;
using Unity.Netcode;

namespace Game.Mechanics.Interactables.Tools
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkObject))]
    public class ItemBehaviour : NetworkBehaviour, IInteractable
    {
        public ItemData data;
        public ItemInstance itemInstance;

        private void Reset()
        {
            itemInstance = new ItemInstance(data);
            var coll = GetComponent<Collider>();
            if (coll) coll.isTrigger = true;
        }

        private void Awake()
        {
            itemInstance = new ItemInstance(data);
        }

        // UI hints
        public void OnFocusEnter(GameObject player) { }
        public void OnFocusExit(GameObject player) { }

        //public void Interact(GameObject localPlayerObject)
        //{
        //    if (!IsOwner && !IsServer)
        //    {
        //        Debug.LogWarning("Interact called by non-owner?");
        //    }

        //    var inv = localPlayerObject.GetComponent<PlayerInventory>();
        //    if (inv == null)
        //    {
        //        Debug.LogWarning("Local player has no PlayerInventory");
        //        return;
        //    }

        //    bool ok = inv.TryPickup(itemInstance);
        //    if (!ok)
        //    {
        //        Debug.Log("Inventory full locally");
        //        return;
        //    }

        //    RequestDespawnServerRpc();
        //}
        public void Interact(GameObject localPlayerObject)
        {
            var inv = localPlayerObject.GetComponent<PlayerInventory>();
            if (inv == null) return;

            inv.RequestPickupServerRpc(new NetworkObjectReference(NetworkObject));
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDespawnServerRpc()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

    }
}
