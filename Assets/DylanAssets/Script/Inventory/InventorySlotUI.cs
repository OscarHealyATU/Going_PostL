using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;

    public ItemData CurrentItem { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    public void SetItem(ItemData item, int slotIndex)
    {
        CurrentItem = item;
        SlotIndex = slotIndex;

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }
    }

    public void SetEmpty()
    {
        CurrentItem = null;
        SlotIndex = -1;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (CurrentItem == null)
            return;

        if (PackingTableUI.Instance == null)
            return;

        PackingTableUI.Instance.TryPlaceFromInventory(CurrentItem, SlotIndex);
    }
}