using UnityEngine;

public class BinUI : MonoBehaviour
{
    public static BinUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private BinSlotUI[] slots;

    [Header("Player Lock")]
    [SerializeField] private PlayerMovementInside playerMovement;
    [SerializeField] private PlayerLook playerLook;

    private ItemData[] binItems;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        binItems = new ItemData[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, i);
        }

        RefreshUI();
        panelRoot.SetActive(false);
    }

    public void Open()
    {
        isOpen = true;
        panelRoot.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void Close()
    {
        ReturnAllToInventory();

        isOpen = false;
        panelRoot.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerLook != null)
            playerLook.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RefreshUI();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RefreshUI();
    }

    public bool TryAddItemFromInventory(ItemData item, int inventorySlotIndex)
    {
        if (!isOpen || item == null || InventoryManager.Instance == null)
            return false;

        int freeSlot = GetFirstEmptySlot();
        if (freeSlot < 0)
            return false;

        binItems[freeSlot] = item;
        InventoryManager.Instance.RemoveItem(inventorySlotIndex);

        RefreshUI();
        return true;
    }

    public void TryReturnItemToInventory(int binSlotIndex)
    {
        if (InventoryManager.Instance == null)
            return;

        if (binSlotIndex < 0 || binSlotIndex >= binItems.Length)
            return;

        ItemData item = binItems[binSlotIndex];
        if (item == null)
            return;

        bool added = InventoryManager.Instance.AddItem(item);
        if (!added)
            return;

        binItems[binSlotIndex] = null;
        RefreshUI();
    }

    public void EmptyBin()
    {
        for (int i = 0; i < binItems.Length; i++)
        {
            ItemData item = binItems[i];
            if (item == null)
                continue;

            DeleteBinItem(item);
            binItems[i] = null;
        }

        RefreshUI();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RefreshUI();

        if (DeliveryManager.Instance != null)
            DeliveryManager.Instance.RefreshDeliveryState();
    }

    private void DeleteBinItem(ItemData item)
    {
        if (item == null)
            return;

        DeliveryJob job = DeliveryService.GetByItemId(item.itemKey);
        if (job == null)
            return;

        DeliveryService.DeleteJob(job.id);
    }

    private void ReturnAllToInventory()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < binItems.Length; i++)
        {
            ItemData item = binItems[i];
            if (item == null)
                continue;

            bool added = InventoryManager.Instance.AddItem(item);
            if (added)
                binItems[i] = null;
        }
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < binItems.Length; i++)
        {
            if (binItems[i] == null)
                return i;
        }

        return -1;
    }

    public void RefreshUI()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (i >= binItems.Length || binItems[i] == null)
                slots[i].SetEmpty();
            else
                slots[i].SetItem(binItems[i]);
        }
    }
}