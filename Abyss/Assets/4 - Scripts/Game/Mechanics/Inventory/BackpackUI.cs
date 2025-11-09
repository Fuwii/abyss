using System.Collections.Generic;
using UnityEngine;

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
        for (int i = 0; i < capacity; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotContainer);
            var slotUI = slotGO.GetComponent<ItemSlotUI>();
            Debug.Log(slotUI);
            slotUI.Setup(playerInventory, i, isBackpack: true);
            slotUIs.Add(slotUI);
        }
    }
}
