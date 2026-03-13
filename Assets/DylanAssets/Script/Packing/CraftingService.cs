public static class CraftingService
{
    public static bool TryPackFirstEligible(int playerId, out string message)
    {
        if (InventoryManager.Instance == null)
        {
            message = "InventoryManager not found.";
            return false;
        }

        int itemSlot = InventoryManager.Instance.GetFirstSlotByCategory("SpawnedItem");
        int boxSlot = InventoryManager.Instance.GetFirstSlotByKey("open_box");

        if (itemSlot < 0)
        {
            message = "You need a spawned item first.";
            return false;
        }

        if (boxSlot < 0)
        {
            message = "You need an open box first.";
            return false;
        }

        return TryPackSpecific(playerId, itemSlot, boxSlot, out message);
    }

    public static bool TryPackSpecific(int playerId, int itemSlotIndex, int boxSlotIndex, out string message)
    {
        if (InventoryManager.Instance == null)
        {
            message = "InventoryManager not found.";
            return false;
        }

        if (ItemCatalog.Instance == null)
        {
            message = "ItemCatalog not found.";
            return false;
        }

        if (itemSlotIndex == boxSlotIndex)
        {
            message = "Item and box must be different slots.";
            return false;
        }

        ItemData sourceItem = InventoryManager.Instance.GetItem(itemSlotIndex);
        ItemData boxItem = InventoryManager.Instance.GetItem(boxSlotIndex);

        if (sourceItem == null)
        {
            message = "Selected item slot is empty.";
            return false;
        }

        if (boxItem == null)
        {
            message = "Selected box slot is empty.";
            return false;
        }

        if (sourceItem.category != "SpawnedItem")
        {
            message = "The first input must be a spawned item.";
            return false;
        }

        if (boxItem.itemKey != "open_box")
        {
            message = "The second input must be an open box.";
            return false;
        }

        ItemData packageItem = ItemCatalog.Instance.GetByKey("closed_package");
        if (packageItem == null)
        {
            message = "closed_package ItemData not found in ItemCatalog.";
            return false;
        }

        InventoryManager.Instance.RemoveItem(itemSlotIndex);
        InventoryManager.Instance.RemoveItem(boxSlotIndex);

        bool added = InventoryManager.Instance.AddItem(packageItem);
        if (!added)
        {
            InventoryManager.Instance.AddItem(sourceItem);
            InventoryManager.Instance.AddItem(boxItem);
            message = "Inventory full. Could not add closed package.";
            return false;
        }

        message = $"Packed {sourceItem.itemName} into a delivery box.";
        return true;
    }
}