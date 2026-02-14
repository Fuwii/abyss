using UnityEngine;
using Game.Player.Stamina;
using Game.Mechanics.Effects;

namespace Game.Mechanics.Interactables.Tools
{
    [CreateAssetMenu(menuName = "Items/Consumable", fileName = "New Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("Consumable")]
        public float restoreStamina = 0f;
        public EffectSourceConfigSO applyEffectOnUse; 
    }

}
