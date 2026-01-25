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
        [Header("Editor / Fallback")]
        [SerializeField] private Transform visualRoot;
        private GameObject runtimeVisual;

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
        //test
        public override void OnNetworkSpawn()
        {
            SpawnVisual();
        }

        void SpawnVisual()
        {
            if (visualRoot == null)
            {
                Debug.LogError("ItemBehaviour: visualRoot not assigned", this);
                return;
            }

            ClearVisuals();

            if (data == null || data.itemPrefab == null)
                return;

            runtimeVisual = Instantiate(data.itemPrefab, visualRoot);
            runtimeVisual.transform.localPosition = Vector3.zero;
            runtimeVisual.transform.localRotation = Quaternion.identity;

            if (runtimeVisual.TryGetComponent<NetworkObject>(out var netObj))
                netObj.enabled = false;
        }

        void ClearVisuals()
        {
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
                Destroy(visualRoot.GetChild(i).gameObject);
        }

    }
}
