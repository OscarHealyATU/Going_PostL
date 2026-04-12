using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BinSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;

    public ItemData CurrentItem { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    private BinUI owner;

    public void Setup(BinUI binUI, int slotIndex)
    {
        owner = binUI;
        SlotIndex = slotIndex;
        SetEmpty();
    }

    public void SetItem(ItemData item)
    {
        CurrentItem = item;

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }
    }

    public void SetEmpty()
    {
        CurrentItem = null;

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

        if (CurrentItem == null || owner == null)
            return;

        owner.TryReturnItemToInventory(SlotIndex);
    }
}