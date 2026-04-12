using UnityEngine;

public class GameInventoryBinBridge : BinInventoryBridge
{
    public override bool TryTakeFromInventorySlot(int inventorySlotIndex, out BinPayload payload)
    {
        payload = null;

        // EXAMPLE ONLY:
        // 1. Read your inventory slot data
        // 2. Detect whether it is an item, box, or packed delivery
        // 3. Remove it from inventory / DB
        // 4. Convert it to BinPayload

        /*
        var slot = InventoryService.GetSlot(inventorySlotIndex);
        if (slot == null || slot.IsEmpty)
            return false;

        payload = new BinPayload
        {
            kind = ConvertKind(slot),
            dataId = slot.dataId,
            quantity = slot.quantity,
            displayName = slot.displayName,
            icon = slot.icon
        };

        InventoryService.ClearSlot(inventorySlotIndex);
        InventoryService.Save();
        return true;
        */

        return false;
    }

    public override bool TryReturnToInventory(BinPayload payload)
    {
        if (payload == null)
            return false;

        // EXAMPLE ONLY:
        // put it back into the player's inventory if there is room

        /*
        bool success = InventoryService.TryAdd(payload.kind, payload.dataId, payload.quantity);
        if (success)
            InventoryService.Save();

        return success;
        */

        return false;
    }

    public override void DeletePayload(BinPayload payload)
    {
        if (payload == null)
            return;

        // EXAMPLE ONLY:
        // if your inventory data was already removed when it entered the bin,
        // then this may only need to save DB / trigger refresh.
        // if not, delete the underlying record here.
    }

    public override void RefreshInventoryUI()
    {
        // EXAMPLE ONLY:
        // InventoryUI.Instance?.Rebuild();
    }
}