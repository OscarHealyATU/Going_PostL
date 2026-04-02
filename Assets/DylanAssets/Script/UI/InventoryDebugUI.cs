using System.Text;
using TMPro;
using UnityEngine;

public class InventoryDebugUI : MonoBehaviour
{
    public TMP_Text debugText;
    public float refreshInterval = 0.2f;

    private float nextRefreshTime;

    void Update()
    {
        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime = Time.time + refreshInterval;
        Refresh();
    }

    void Refresh()
    {
        if (debugText == null)
            return;

        if (InventoryManager.Instance == null)
        {
            debugText.text = "InventoryManager not found.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Inventory Debug");

        for (int i = 0; i < InventoryManager.Instance.items.Length; i++)
        {
            ItemData item = InventoryManager.Instance.items[i];

            if (item == null)
                sb.AppendLine($"Slot {i}: Empty");
            else
                sb.AppendLine($"Slot {i}: {item.itemName} ({item.itemKey})");
        }

        debugText.text = sb.ToString();
    }
}