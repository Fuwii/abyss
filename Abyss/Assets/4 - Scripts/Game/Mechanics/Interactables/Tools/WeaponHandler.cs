using Game.Mechanics.Interactables.Tools;
using UnityEngine;

public class WeaponHandler : ItemHandler
{
    public override void OnUse(GameObject player, ItemInstance instance)
    {
        base.OnUse(player, instance); // сохраняем стандартное поведение (UseOne)

        var wd = instance.itemData as WeaponData;
        if (wd == null) return;
        //attack logic
    }
}

