using System.Collections;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 3;

    [Header("Current Items")]
    [SerializeField] private ItemData[] items;

    private bool hasLoadedFromDatabase = false;

    public int MaxSlots => maxSlots;
    public ItemData[] Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyCapacityFromPlayer();

        Debug.Log($"[InventoryManager] Awake maxSlots={maxSlots}, items.Length={(items != null ? items.Length : 0)}");
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(WaitForSystemsAndLoad());
    }

    private IEnumerator WaitForSystemsAndLoad()
    {
        while (DbBoot.Instance == null)
            yield return null;

        while (ItemCatalog.Instance == null)
            yield return null;

        ApplyCapacityFromPlayer();

        if (!hasLoadedFromDatabase)
        {
            LoadFromDatabase();
            hasLoadedFromDatabase = true;
            RefreshUI();
        }
    }

    private void ApplyCapacityFromPlayer()
    {
        int desiredSlots = PlayerService.GetInventorySlotCount();
        if (desiredSlots <= 0)
            desiredSlots = 3;

        if (items == null)
        {
            items = new ItemData[desiredSlots];
        }
        else if (items.Length != desiredSlots)
        {
            ItemData[] resized = new ItemData[desiredSlots];
            int copyCount = Mathf.Min(items.Length, desiredSlots);

            for (int i = 0; i < copyCount; i++)
                resized[i] = items[i];

            items = resized;
        }

        maxSlots = desiredSlots;
    }

    public void RefreshCapacityFromPlayer()
    {
        int oldMaxSlots = maxSlots;

        ApplyCapacityFromPlayer();

        Debug.Log($"[InventoryManager] Capacity refreshed from {oldMaxSlots} to {maxSlots}");

        SaveToDatabase();
        RefreshUI();
    }

    public bool AddItem(ItemData item)
    {
        if (item == null)
            return false;

        ApplyCapacityFromPlayer();

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null)
                continue;

            items[i] = item;
            SaveToDatabase();
            RefreshUI();

            Debug.Log($"[InventoryManager] Added '{item.itemName}' to slot {i}");
            return true;
        }

        Debug.Log($"[InventoryManager] Inventory full, could not add '{item.itemName}'");
        return false;
    }

    public void RemoveItem(int slotIndex)
    {
        ApplyCapacityFromPlayer();

        if (items == null || slotIndex < 0 || slotIndex >= maxSlots)
            return;

        items[slotIndex] = null;
        SaveToDatabase();
        RefreshUI();
    }

    public ItemData GetItem(int slotIndex)
    {
        ApplyCapacityFromPlayer();

        if (items == null || slotIndex < 0 || slotIndex >= maxSlots)
            return null;

        return items[slotIndex];
    }

    public int GetFirstSlotByCategory(string category)
    {
        ApplyCapacityFromPlayer();

        if (items == null)
            return -1;

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null && items[i].category == category)
                return i;
        }

        return -1;
    }

    public int GetFirstSlotByKey(string itemKey)
    {
        ApplyCapacityFromPlayer();

        if (items == null)
            return -1;

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null && items[i].itemKey == itemKey)
                return i;
        }

        return -1;
    }

    public bool RemoveFirstMatchingItem(string itemKey)
    {
        ApplyCapacityFromPlayer();

        if (string.IsNullOrEmpty(itemKey) || items == null)
            return false;

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemKey != itemKey)
                continue;

            items[i] = null;
            SaveToDatabase();
            RefreshUI();
            return true;
        }

        return false;
    }

    public void SaveToDatabase()
    {
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] DbBoot not found.");
            return;
        }

        var player = PlayerService.Get();
        if (player == null)
        {
            Debug.LogWarning("[InventoryManager] PlayerService.Get() returned null.");
            return;
        }

        ApplyCapacityFromPlayer();

        var db = DbBoot.Instance.Db;

        var existingRows = db.Table<InventorySlot>()
            .Where(s => s.playerId == player.id)
            .ToList();

        for (int i = 0; i < existingRows.Count; i++)
            db.Delete(existingRows[i]);

        int insertedCount = 0;

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] == null)
                continue;

            if (string.IsNullOrWhiteSpace(items[i].itemKey))
            {
                Debug.LogWarning($"[InventoryManager] Slot {i}: itemKey blank, skipping insert.");
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

        Debug.Log($"[InventoryManager] Saved inventory. Capacity={maxSlots}, Inserted={insertedCount}");
    }

    public void LoadFromDatabase()
    {
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] DbBoot not found.");
            return;
        }

        if (ItemCatalog.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] ItemCatalog not found.");
            return;
        }

        var player = PlayerService.Get();
        if (player == null)
        {
            Debug.LogWarning("[InventoryManager] PlayerService.Get() returned null.");
            return;
        }

        ApplyCapacityFromPlayer();

        for (int i = 0; i < maxSlots; i++)
            items[i] = null;

        var db = DbBoot.Instance.Db;

        var savedSlots = db.Table<InventorySlot>()
            .Where(s => s.playerId == player.id && s.slotIndex >= 0 && s.slotIndex < maxSlots)
            .OrderBy(s => s.slotIndex)
            .ToList();

        foreach (var row in savedSlots)
        {
            ItemData item = ItemCatalog.Instance.GetByKey(row.itemKey);

            if (item == null)
            {
                Debug.LogWarning($"[InventoryManager] No ItemData found for key '{row.itemKey}'");
                continue;
            }

            items[row.slotIndex] = item;
        }

        // Rewrite DB so any rows above current capacity are removed.
        SaveToDatabase();

        Debug.Log($"[InventoryManager] Loaded inventory. Capacity={maxSlots}");
    }

    public void RefreshUI()
    {
        InventoryUI ui = FindFirstObjectByType<InventoryUI>();
        if (ui != null)
            ui.RefreshUI();
    }
}