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
    }
}
