using Game.Mechanics.Interactables.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PlayerInventory;

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
    private static ItemSlotUI dragSourceSlot;

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
        if (inventory == null || !inventory.IsOwner) return;
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
        // ¬ажно: берем данные из статической переменной
        ItemSlotUI from = dragSourceSlot;

        // ≈сли "откуда" пусто или это тот же слот Ч выходим
        if (from == null || from == this) return;

        // ѕ–ќ¬≈– ј: ћожем ли мы взаимодействовать с этим инвентарем
        if (inventory == null || !inventory.IsOwner) return;

        InventorySource fromSource = from.isBackpack ? InventorySource.Backpack : InventorySource.PlayerInventory;
        InventorySource toSource = isBackpack ? InventorySource.Backpack : InventorySource.PlayerInventory;

        // ¬ызываем RPC
        inventory.MoveItemServerRpc(fromSource, from.slotIndex, toSource, slotIndex);

        CleanupDragIcon();
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
