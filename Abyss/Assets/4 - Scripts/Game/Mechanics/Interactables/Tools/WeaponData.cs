using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    [CreateAssetMenu(menuName = "Items/Weapon", fileName = "New Weapon")]
    public class WeaponData : ItemData
    {
        [Header("Weapon")]
        public int damage = 1;
        public float attackRate = 1f;
        public float range = 1f;
        public bool twoHanded = false;

        public override void OnSelected(GameObject player, ItemInstance instance)
        {
            var hand = player.transform.Find("Hand_R"); 
            if (hand != null && itemPrefab != null)
            {
                var go = GameObject.Instantiate(itemPrefab, hand);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                instance.runtimeHeldObject = go;
            }
        }

        public override void OnUse(GameObject player, ItemInstance instance)
        {
            //Some attack not created yet

            if (instance != null)
                instance.UseOne();
        }
    }
}
