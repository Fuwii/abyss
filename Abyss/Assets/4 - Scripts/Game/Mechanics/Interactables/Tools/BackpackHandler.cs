using Game.Mechanics.Interactables.Tools;
using UnityEngine;

public class BackpackHandler : ItemHandler
{
    public override void OnSelected(GameObject player, ItemInstance instance, Transform handTransform)
    {
        base.OnSelected(player, instance, handTransform);

        if (instance == null) return;
        var bp = instance.GetComponent<BackpackComponent>();
        if (bp == null) return; 

        if (bp.activeUIInstance != null) return;

        if (bp.uiPrefab == null)
        {
            Debug.LogWarning("BackpackHandler: uiPrefab is null on BackpackComponent.");
            return;
        }
        Canvas targetCanvas = player.GetComponentInChildren<Canvas>();
        Transform parent = targetCanvas != null ? targetCanvas.transform : null;

        bp.activeUIInstance = Object.Instantiate(bp.uiPrefab, parent);
        var ui = bp.activeUIInstance.GetComponent<BackpackUI>();
        if (ui != null)
        {
            var inv = player.GetComponent<PlayerInventory>();
            ui.playerInventory = inv;
            ui.gameObject.SetActive(true);
            ui.RefreshBackpack();
        }
    }

    public override void OnDeselected(GameObject player, ItemInstance instance)
    {
        base.OnDeselected(player, instance);

        if (instance == null) return;
        var bp = instance.GetComponent<BackpackComponent>();
        if (bp == null) return;

        if (bp.activeUIInstance != null)
        {
            Object.Destroy(bp.activeUIInstance);
            bp.activeUIInstance = null;
        }
    }
}
