using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Mechanics.Interactables.Tools;

/// <summary>
/// Универсальный UI слот (может быть как для main, так и для backpack).
/// Скрипт должен висеть на GameObject слота (обычно на Button).
/// Icon — дочерний Image, куда рисуется спрайт предмета.
/// Метод Setup(...) вызывается при создании слота из BackpackUI/InventoryUI.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("References")]
    public Image icon;          // перетащить сюда дочерний Image
    public PlayerInventory inventory; // может быть установлен через Setup или вручную в инспекторе

    [Header("Slot config")]
    public int slotIndex = 0;
    public bool isBackpack = false;

    // runtime
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // drag state
    private GameObject dragIcon;
    private ItemSlotUI dragSourceSlot;

    private bool subscribedToInventory = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // ищем Canvas наверху
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            canvasRect = rootCanvas.GetComponent<RectTransform>();

        // получим/создадим CanvasGroup — это безопасно
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Если inventory уже задан (через инспектор или Setup), подпишемся
        TrySubscribeInventory();
        Refresh();
    }

    private void OnDestroy()
    {
        UnsubscribeInventory();
        if (dragIcon != null)
            Destroy(dragIcon);
    }

    /// <summary>
    /// Вызывается внешним кодом (BackpackUI, InventoryUI) сразу после Instantiate(slotPrefab).
    /// Настраивает слот на нужный inventory/index/type и подписывает на обновления.
    /// </summary>
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

    /// <summary>
    /// Вызывай чтобы обновить визуал слота.
    /// </summary>
    public void Refresh()
    {
        if (inventory == null || icon == null)
        {
            if (icon != null) { icon.enabled = false; icon.sprite = null; }
            return;
        }

        ItemInstance inst = isBackpack ? inventory.GetBackpackSlot(slotIndex) : inventory.GetMainSlot(slotIndex);
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
        // Не начинаем drag, если слот пустой
        if (inventory == null) return;
        ItemInstance inst = isBackpack ? inventory.GetBackpackSlot(slotIndex) : inventory.GetMainSlot(slotIndex);
        if (inst == null || inst.itemData == null) return;

        dragSourceSlot = this;

        if (rootCanvas == null)
        {
            Debug.LogWarning("ItemSlotUI: no Canvas found in parents — cannot start drag.");
            return;
        }

        // создаём drag icon в корне канвы
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;
        var rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = rectTransform.sizeDelta;

        // затемняем оригинал
        if (canvasGroup != null)
            canvasGroup.alpha = 0.6f;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            Destroy(dragIcon);
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
        // Определяем источник: либо через dragSourceSlot, либо через pointerDrag
        ItemSlotUI from = dragSourceSlot;
        if (from == null && eventData.pointerDrag != null)
            from = eventData.pointerDrag.GetComponent<ItemSlotUI>();

        if (from == null || from == this) return;
        if (inventory == null) return;

        // Сценарии:
        if (isBackpack && from.isBackpack)
        {
            inventory.SwapBackpackSlots(from.slotIndex, slotIndex);
            return;
        }

        if (!isBackpack && from.isBackpack)
        {
            MoveFromBackpackToMain(from.slotIndex, slotIndex);
            return;
        }

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
        if (inventory.GetBackpackSlot(backpackIndex) != null) return;

        var taken = inventory.RemoveFromMain(mainIndex);
        if (taken != null) inventory.TryPutIntoBackpack(backpackIndex, taken);
    }

    private void MoveFromBackpackToMain(int backpackIndex, int mainIndex)
    {
        if (!inventory.backpackWorn) return;
        var item = inventory.GetBackpackSlot(backpackIndex);
        if (item == null) return;
        if (inventory.GetMainSlot(mainIndex) != null) return;

        var taken = inventory.DropFromBackpack(backpackIndex);
        if (taken != null) inventory.TryPutIntoMain(mainIndex, taken);
    }
}
