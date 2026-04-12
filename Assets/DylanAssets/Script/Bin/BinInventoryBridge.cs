using UnityEngine;

public abstract class BinInventoryBridge : MonoBehaviour
{
    /// <summary>
    /// Remove the content from a player inventory slot and convert it into a bin payload.
    /// Return true only if something was successfully removed.
    /// </summary>
    public abstract bool TryTakeFromInventorySlot(int inventorySlotIndex, out BinPayload payload);

    /// <summary>
    /// Return a payload from the bin back into the player's inventory.
    /// Return true only if the inventory accepted it.
    /// </summary>
    public abstract bool TryReturnToInventory(BinPayload payload);

    /// <summary>
    /// Permanently delete the payload.
    /// Use this to update DB / runtime state.
    /// </summary>
    public abstract void DeletePayload(BinPayload payload);

    /// <summary>
    /// Refresh any inventory UI after changes.
    /// </summary>
    public abstract void RefreshInventoryUI();
}