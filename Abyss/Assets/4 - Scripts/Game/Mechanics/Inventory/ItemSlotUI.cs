using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Mechanics.Interactables.Tools;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
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
        CleanupDragIcon();
    }

    private void OnDisable()
    {
        // ≈сли объект деактивируетс€ во врем€ драг-н-дропа Ч очистим
        CleanupDragIcon();
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
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
            // ѕодпишемс€ на все событи€ Ч это безопасно и полезно дл€ обновлени€ UI
            inventory.OnInventoryChanged += Refresh;
            // ≈сли у теб€ есть отдельное событие дл€ рюкзака Ч подпишись тоже (если нет, безопасно убрать)
            // inventory.OnBackpackChanged += Refresh;
            subscribedToInventory = true;
        }
    }

    private void UnsubscribeInventory()
    {
        if (inventory != null && subscribedToInventory)
        {
            inventory.OnInventoryChanged -= Refresh;
            // inventory.OnBackpackChanged -= Refresh;
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

        // »спользуем текущие методы инвентар€ (как в твоЄм новом коде)
        ItemInstance inst = isBackpack ? inventory.GetBackpackContentsAt(slotIndex) : inventory.GetSlot(slotIndex);
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
        ItemInstance inst = isBackpack ? inventory.GetBackpackContentsAt(slotIndex) : inventory.GetSlot(slotIndex);
        if (inst == null || inst.itemData == null) return;

        // если осталс€ старый Ч уничтожаем (защита от дубликатов)
        if (dragIcon != null) CleanupDragIcon();

        dragSourceSlot = this;
        if (rootCanvas == null)
        {
            Debug.LogWarning("ItemSlotUI: no Canvas found in parents Ч cannot start drag.");
            return;
        }

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIcon.transform.SetAsLastSibling();
        dragIcon.transform.localScale = Vector3.one;

        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false; // важно: иконка не должна перехватывать событи€

        // чтобы иконка точно не мешала событи€м Ч добавим CanvasGroup и выключим блокировку лучей
        var cg = dragIcon.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

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
        // OnEndDrag может не вызватьс€ Ч но если вызвалс€, соберЄмс€ корректно
        CleanupDragIcon();
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (rootCanvas == null || dragIcon == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, rootCanvas.worldCamera, out Vector2 pos);
        dragIcon.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // ѕри любом OnDrop тоже делаем чистку (страховка)
        CleanupDragIcon();

        ItemSlotUI from = dragSourceSlot;
        if (from == null && eventData.pointerDrag != null)
            from = eventData.pointerDrag.GetComponent<ItemSlotUI>();

        if (from == null || from == this) return;
        if (inventory == null) return;

        // backpack <-> backpack (swap contents)
        if (isBackpack && from.isBackpack)
        {
            if (inventory.GetBackpackComponent() == null) return;
            inventory.SwapBackpackSlots(from.slotIndex, slotIndex);
            return;
        }

        // main <- backpack  (move backpack content -> main global slot)
        if (!isBackpack && from.isBackpack)
        {
            MoveFromBackpackToMain(from.slotIndex, slotIndex);
            return;
        }

        // backpack <- main (move main global slot -> backpack content)
        if (isBackpack && !from.isBackpack)
        {
            MoveFromMainToBackpack(from.slotIndex, slotIndex);
            return;
        }

        // main <-> main swap (global inventory slots)
        if (!isBackpack && !from.isBackpack)
        {
            inventory.SwapSlots(from.slotIndex, slotIndex);
            return;
        }
    }

    // helpers
    private void MoveFromMainToBackpack(int mainIndex, int backpackContentIndex)
    {
        if (!inventory.backpackWorn) return;
        var item = inventory.GetSlot(mainIndex);
        if (item == null) return;
        if (inventory.GetBackpackContentsAt(backpackContentIndex) != null) return;

        var taken = inventory.RemoveFromSlot(mainIndex);
        if (taken != null) inventory.TryPutIntoBackpack(backpackContentIndex, taken);
    }

    private void MoveFromBackpackToMain(int backpackContentIndex, int mainIndex)
    {
        if (!inventory.backpackWorn) return;
        var item = inventory.GetBackpackContentsAt(backpackContentIndex);
        if (item == null) return;
        if (inventory.GetSlot(mainIndex) != null) return;

        var taken = inventory.DropFromBackpack(backpackContentIndex);
        if (taken != null) inventory.TryPutIntoSlot(mainIndex, taken);
    }

    // ќчистка drag-объекта и восстановление состо€ни€ слота
    private void CleanupDragIcon()
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        dragSourceSlot = null;
    }

    private void LateUpdate()
    {
        // fallback: если мы держали иконку, а кнопка мыши отпущена Ч гарантированно очистим.
        if (dragIcon != null && !Input.GetMouseButton(0))
        {
            CleanupDragIcon();
        }
    }
}
