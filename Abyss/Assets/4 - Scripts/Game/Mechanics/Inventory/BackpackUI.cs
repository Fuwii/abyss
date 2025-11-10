using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public Transform slotContainer;       
    public GameObject slotPrefab;         

    private List<ItemSlotUI> slotUIs = new();

    private void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("BackpackUI: PlayerInventory not assigned!");
            return;
        }

        playerInventory.OnBackpackChanged += RefreshBackpack;
        playerInventory.OnInventoryChanged += RefreshBackpack;

        RefreshBackpack();
    }

    private void OnDestroy()
    {
        playerInventory.OnBackpackChanged -= RefreshBackpack;
        playerInventory.OnInventoryChanged -= RefreshBackpack;
    }

    private void ClearSlots()
    {
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        slotUIs.Clear();
    }

    public void RefreshBackpack()
    {
        ClearSlots();

        if (!playerInventory.backpackWorn)
            return;

        int capacity = playerInventory.GetBackpackSize();
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(capacity)); 
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
        }

        for (int i = 0; i < capacity; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotContainer);
            var slotUI = slotGO.GetComponent<ItemSlotUI>();
            slotUI.Setup(playerInventory, i, isBackpack: true);
            slotUIs.Add(slotUI);
        }
    }
}
