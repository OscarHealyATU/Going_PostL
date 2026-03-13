using UnityEngine;
using UnityEngine.UI;

public class PackingSlotUI : MonoBehaviour
{
    [Header("Rules")]
    public PackingSlotType slotType;

    [Header("UI")]
    public Image iconImage;

    private ItemData _item;

    public ItemData Item => _item;
    public bool HasItem => _item != null;

    public bool CanAccept(ItemData item)
    {
        if (item == null) return false;

        switch (slotType)
        {
            case PackingSlotType.ItemInput:
                return item.itemKey != "open_box" && !item.itemKey.StartsWith("packed_");

            case PackingSlotType.BoxInput:
                return item.itemKey == "open_box";

            case PackingSlotType.Result:
                return false;
        }

        return false;
    }

    public void SetItem(ItemData item)
    {
        _item = item;
        RefreshVisual();
    }

    public void ClearSlot()
    {
        _item = null;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (iconImage == null) return;

        if (_item != null && _item.icon != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = _item.icon;
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
    }
}

public enum PackingSlotType
{
    ItemInput,
    BoxInput,
    Result
}