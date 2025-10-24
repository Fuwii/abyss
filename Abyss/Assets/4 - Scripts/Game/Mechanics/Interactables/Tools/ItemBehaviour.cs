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
            //some inventory logic
            data.OnPickup(player, instance);

            Destroy(gameObject);
        }
    }
}
