using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PackingSlotUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public enum SlotType
    {
        Item,
        Box,
        Result
    }

    [Header("Setup")]
    public SlotType slotType;
    public Image icon;

    public ItemData CurrentItem { get; private set; }
    public int SourceInventorySlotIndex { get; private set; } = -1;

    public void SetHeldItem(ItemData item, int sourceSlotIndex)
    {
        CurrentItem = item;
        SourceInventorySlotIndex = sourceSlotIndex;

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }
    }

    public void SetResultItem(ItemData item)
    {
        CurrentItem = item;
        SourceInventorySlotIndex = -1;

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }
    }

    public void ClearVisualOnly()
    {
        CurrentItem = null;
        SourceInventorySlotIndex = -1;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //debug.Log("POINTER DOWN on " + name + " button: " + eventData.button);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //debug.Log("POINTER CLICK on " + name + " button: " + eventData.button);

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (CurrentItem == null)
            return;

        if (PackingTableUI.Instance == null)
            return;

        PackingTableUI.Instance.ReturnSlotToInventory(this);
    }
}