using UnityEngine;

public static class PackingService
{
    public static ItemData TryPack(ItemData item, ItemData box, ItemData closedBoxItem)
    {
        if (item == null || box == null || closedBoxItem == null)
            return null;

        if (box.itemKey != "open_box")
            return null;

        return closedBoxItem;
    }
}