using UnityEngine;

//item in word
namespace Game.Mechanics.Interactables.Tools
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ItemBehaviour : MonoBehaviour, IInteractable
    {
        public ItemData data;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        //ui hints(price,name etc)
        public void OnFocusEnter(GameObject player) {}
        public void OnFocusExit(GameObject player) {}

        public void Interact(GameObject player)
        {
            if (data == null) return;


            var instance = new ItemInstance(data);


            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                Debug.LogWarning("Player has no PlayerInventory component");
                data.OnPickup(player, instance);
                Destroy(gameObject);
                return;
            }


            bool ok = inv.TryPickup(instance);
            if (ok)
            {
                data.OnPickup(player, instance);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory full");
            }
        }
    }
}
