using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    public int maxSlots = 10;

    [Header("Current Items")]
    public ItemData[] items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        items = new ItemData[maxSlots];
    }

    private System.Collections.IEnumerator Start()
    {
        while (DbBoot.Instance == null || ItemCatalog.Instance == null)
            yield return null;

        LoadFromDatabase();
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                SaveToDatabase();
                return true;
            }
        }

        return false;
    }

    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Length) return;

        items[slotIndex] = null;
        SaveToDatabase();
    }

    public ItemData GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Length) return null;
        return items[slotIndex];
    }

    public int GetFirstSlotByCategory(string category)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].category == category)
                return i;
        }

        return -1;
    }

    public int GetFirstSlotByKey(string itemKey)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemKey == itemKey)
                return i;
        }

        return -1;
    }

    public void SaveToDatabase()
{
    if (DbBoot.Instance == null)
    {
        Debug.LogWarning("InventoryManager: DbBoot not found.");
        return;
    }

    var player = PlayerService.Get();
    if (player == null)
    {
        Debug.LogWarning("InventoryManager: PlayerService.Get() returned null.");
        return;
    }

    var db = DbBoot.Instance.Db;

    var existingRows = db.Table<InventorySlot>()
        .Where(s => s.playerId == player.id)
        .ToList();

    Debug.Log("Existing inventory rows before delete: " + existingRows.Count);

    for (int i = 0; i < existingRows.Count; i++)
        db.Delete(existingRows[i]);

    int insertedCount = 0;

    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] == null)
        {
            Debug.Log($"Slot {i}: empty");
            continue;
        }

        Debug.Log($"Slot {i}: itemName={items[i].itemName}, itemKey={items[i].itemKey}");

        if (string.IsNullOrWhiteSpace(items[i].itemKey))
        {
            Debug.LogWarning($"Slot {i}: itemKey is blank, skipping DB insert.");
            continue;
        }

        db.Insert(new InventorySlot
        {
            playerId = player.id,
            slotIndex = i,
            itemKey = items[i].itemKey,
            itemName = items[i].itemName
        });

        insertedCount++;
    }

    Debug.Log("Inventory saved to DB. Inserted rows: " + insertedCount);
}

    public void LoadFromDatabase()
    {
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("InventoryManager: DbBoot not found.");
            return;
        }

        if (ItemCatalog.Instance == null)
        {
            Debug.LogWarning("InventoryManager: ItemCatalog not found.");
            return;
        }

        var player = PlayerService.Get();
        var db = DbBoot.Instance.Db;

        // Clear runtime inventory first
        for (int i = 0; i < items.Length; i++)
            items[i] = null;

        var savedSlots = db.Table<InventorySlot>()
            .Where(s => s.playerId == player.id)
            .OrderBy(s => s.slotIndex)
            .ToList();

        for (int i = 0; i < savedSlots.Count; i++)
        {
            var row = savedSlots[i];

            if (row.slotIndex < 0 || row.slotIndex >= items.Length)
                continue;

            ItemData item = ItemCatalog.Instance.GetByKey(row.itemKey);
            if (item != null)
                items[row.slotIndex] = item;
        }

        Debug.Log("Inventory loaded from DB.");
    }
}