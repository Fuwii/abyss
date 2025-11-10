using UnityEngine;

//item in word
namespace Game.Mechanics.Interactables.Tools
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ItemBehaviour : MonoBehaviour, IInteractable
    {
        public ItemData data;
        public ItemInstance itemInstance;

        private void Reset()
        {
            itemInstance = new ItemInstance(data);
            GetComponent<Collider>().isTrigger = true;
        }
        private void Awake()
        {
            itemInstance = new ItemInstance(data);
        }
        //ui hints(price,name etc)
        public void OnFocusEnter(GameObject player) {}
        public void OnFocusExit(GameObject player) {}

        public void Interact(GameObject player)
        {
            if (data == null) return;


            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                Debug.LogWarning("Player has no PlayerInventory component");
                return;
            }
            bool ok = inv.TryPickup(itemInstance);
            if (ok)
            {
                Debug.Log("Picked");
                Debug.Log(ItemSystem.Instance);
                ItemSystem.Instance.HandlePickup(player, itemInstance);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory full");
            }
        }
    }
}
