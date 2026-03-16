using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Slots")]
    public List<InventorySlotUI> slots = new List<InventorySlotUI>();

    [Header("Refresh")]
    public float refreshInterval = 0.2f;

    private float nextRefresh;

    private void Update()
    {
        if (Time.time < nextRefresh)
            return;

        nextRefresh = Time.time + refreshInterval;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            ItemData item = InventoryManager.Instance.GetItem(i);

            if (item != null)
                slots[i].SetItem(item, i);
            else
                slots[i].SetEmpty();
        }
    }
}