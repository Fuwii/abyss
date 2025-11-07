using Game.Mechanics.Interactables.Tools;
using Game.Player.Stamina;
using UnityEngine;

public class ConsumableHandler : ItemHandler
{
    public override void OnUse(GameObject player, ItemInstance instance)
    {
        var cd = instance.itemData as ConsumableData;
        if (cd == null) return;

        var ps = player.GetComponent<PlayerStamina>();
        if (ps != null && cd.restoreStamina > 0f)
            ps.AddStamina(cd.restoreStamina);

        if (cd.applyEffectOnUse != null && ps != null)
            ps.ApplyEffect(cd.applyEffectOnUse.ToConfig());

        instance.UseOne();
    }
}
