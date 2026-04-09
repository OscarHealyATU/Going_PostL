using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject basePanel;
    [SerializeField] private GameObject upgradePanel;

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

        int maxSlots = InventoryManager.Instance.MaxSlots;
        bool hasUpgradeSlots = maxSlots > 3;

        if (basePanel != null)
            basePanel.SetActive(true);

        if (upgradePanel != null)
            upgradePanel.SetActive(hasUpgradeSlots);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            bool slotIsValid = i < maxSlots;
            slots[i].gameObject.SetActive(slotIsValid);

            if (!slotIsValid)
                continue;

            ItemData item = InventoryManager.Instance.GetItem(i);

            if (item != null)
                slots[i].SetItem(item, i);
            else
                slots[i].SetEmpty();
        }
    }
}