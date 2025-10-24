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

        public override void OnUse(GameObject player, ItemInstance instance)
        {
            var ps = player.GetComponent<PlayerStamina>();
            if (ps != null && restoreStamina > 0f)
            {
                ps.AddStamina(restoreStamina);
            }

            if (applyEffectOnUse != null && ps != null)
            {
                ps.ApplyEffect(applyEffectOnUse.ToConfig());
            }

            if (instance != null)
            {
                instance.UseOne();
            }
        }
    }

}
