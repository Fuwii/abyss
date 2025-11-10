using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Mechanics.Interactables.Tools;

[RequireComponent(typeof(RectTransform))]
public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("References")]
    public Image icon;
    public PlayerInventory inventory;

    [Header("Slot config")]
    public int slotIndex = 0;
    public bool isBackpack = false;

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private GameObject dragIcon;
    private ItemSlotUI dragSourceSlot;

    private bool subscribedToInventory = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) canvasRect = rootCanvas.GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        TrySubscribeInventory();
        Refresh();
    }

    private void OnDestroy()
    {
        UnsubscribeInventory();
        if (dragIcon != null) Destroy(dragIcon);
    }

    public void Setup(PlayerInventory inv, int index, bool isBackpack)
    {
        inventory = inv;
        slotIndex = index;
        this.isBackpack = isBackpack;

        TrySubscribeInventory();
        Refresh();
    }

    private void TrySubscribeInventory()
    {
        if (inventory != null && !subscribedToInventory)
        {
            inventory.OnInventoryChanged += Refresh;
            inventory.OnBackpackChanged += Refresh;
            subscribedToInventory = true;
        }
    }

    private void UnsubscribeInventory()
    {
        if (inventory != null && subscribedToInventory)
        {
            inventory.OnInventoryChanged -= Refresh;
            inventory.OnBackpackChanged -= Refresh;
            subscribedToInventory = false;
        }
    }

    public void Refresh()
    {
        if (inventory == null || icon == null)
        {
            if (icon != null) { icon.enabled = false; icon.sprite = null; }
            return;
        }

        // ƒл€ backpack используем GetBackpackContentsAt, дл€ main Ч GetMainSlot
        ItemInstance inst = isBackpack ? inventory.GetBackpackContentsAt(slotIndex) : inventory.GetMainSlot(slotIndex);
        if (inst != null && inst.itemData != null && inst.itemData.icon != null)
        {
            icon.sprite = inst.itemData.icon;
            icon.enabled = true;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    // ---------------- Drag n Drop ----------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null) return;
        ItemInstance inst = isBackpack ? inventory.GetBackpackContentsAt(slotIndex) : inventory.GetMainSlot(slotIndex);
        if (inst == null || inst.itemData == null) return;

        dragSourceSlot = this;
        if (rootCanvas == null)
        {
            Debug.LogWarning("ItemSlotUI: no Canvas found in parents Ч cannot start drag.");
            return;
        }

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;
        var rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = rectTransform.sizeDelta;

        if (canvasGroup != null) canvasGroup.alpha = 0.6f;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);
        dragIcon = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        dragSourceSlot = null;
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (rootCanvas == null || dragIcon == null) return;
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, rootCanvas.worldCamera, out pos);
        dragIcon.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI from = dragSourceSlot;
        if (from == null && eventData.pointerDrag != null)
            from = eventData.pointerDrag.GetComponent<ItemSlotUI>();

        if (from == null || from == this) return;
        if (inventory == null) return;

        // backpack <-> backpack
        if (isBackpack && from.isBackpack)
        {
            inventory.SwapBackpackSlots(from.slotIndex, slotIndex);
            return;
        }

        // main <- backpack
        if (!isBackpack && from.isBackpack)
        {
            MoveFromBackpackToMain(from.slotIndex, slotIndex);
            return;
        }

        // backpack <- main
        if (isBackpack && !from.isBackpack)
        {
            MoveFromMainToBackpack(from.slotIndex, slotIndex);
            return;
        }

        // main <-> main swap
        if (!isBackpack && !from.isBackpack)
        {
            var a = inventory.RemoveFromMain(from.slotIndex);
            var b = inventory.RemoveFromMain(slotIndex);
            if (a != null) inventory.TryPutIntoMain(slotIndex, a);
            if (b != null) inventory.TryPutIntoMain(from.slotIndex, b);
            return;
        }
    }

    // helpers
    private void MoveFromMainToBackpack(int mainIndex, int backpackIndex)
    {
        if (!inventory.backpackWorn) return;
        var item = inventory.GetMainSlot(mainIndex);
        if (item == null) return;
        if (inventory.GetBackpackContentsAt(backpackIndex) != null) return;

        var taken = inventory.RemoveFromMain(mainIndex);
        if (taken != null) inventory.TryPutIntoBackpack(backpackIndex, taken);
    }

    private void MoveFromBackpackToMain(int backpackIndex, int mainIndex)
    {
        if (!inventory.backpackWorn) return;
        var item = inventory.GetBackpackContentsAt(backpackIndex);
        if (item == null) return;
        if (inventory.GetMainSlot(mainIndex) != null) return;

        var taken = inventory.DropFromBackpack(backpackIndex);
        if (taken != null) inventory.TryPutIntoMain(mainIndex, taken);
    }
}
