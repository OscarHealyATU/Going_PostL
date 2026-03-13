using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public int slotIndex;
    public Image itemIcon;

    public void SetEmpty()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    public void SetItem(Sprite icon)
    {
        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = icon;
        }
    }
}