using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackingTableUI : MonoBehaviour
{
    public static PackingTableUI Instance { get; private set; }

    [Header("Slots")]
    public PackingSlotUI itemSlot;
    public PackingSlotUI boxSlot;
    public PackingSlotUI resultSlot;

    [Header("Crafting Result")]
    public ItemData closedBoxItem;

    [Header("Buttons")]
    public Button packButton;
    public Button closeButton;

    [Header("Feedback")]
    public TMP_Text errorText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (packButton != null)
            packButton.onClick.AddListener(OnPackClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (packButton != null)
            packButton.onClick.RemoveListener(OnPackClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        ClearError();
    }

   public void ClosePanel()
    {
        ReturnInputsToInventory();

        if (resultSlot != null && resultSlot.CurrentItem != null)
            ReturnSlotToInventory(resultSlot);

        ClearAllSlots();
        ClearError();
        gameObject.SetActive(false);
    }

    public bool TryPlaceFromInventory(ItemData item, int inventorySlotIndex)
    {
        if (!gameObject.activeInHierarchy)
            return false;

        if (item == null || InventoryManager.Instance == null)
            return false;

        ClearError();

        if (resultSlot != null && resultSlot.CurrentItem != null)
        {
            SetError("Take the packed box first.");
            return false;
        }

        if (item.itemKey == "box_open")
        {
            if (boxSlot == null)
            {
                SetError("Box slot is not assigned.");
                return false;
            }

            if (boxSlot.CurrentItem != null)
            {
                SetError("Box slot is already occupied.");
                return false;
            }

            boxSlot.SetHeldItem(item, inventorySlotIndex);
            InventoryManager.Instance.RemoveItem(inventorySlotIndex);
            return true;
        }

        if (!IsPackable(item))
        {
            SetError("That item cannot be packed.");
            return false;
        }

        if (itemSlot == null)
        {
            SetError("Item slot is not assigned.");
            return false;
        }

        if (itemSlot.CurrentItem != null)
        {
            SetError("Item slot is already occupied.");
            return false;
        }

        itemSlot.SetHeldItem(item, inventorySlotIndex);
        InventoryManager.Instance.RemoveItem(inventorySlotIndex);
        return true;
    }

    private bool IsPackable(ItemData item)
    {
        if (item == null)
            return false;

        if (item.itemKey == "box_open")
            return false;

        if (item.itemKey == "box_close")
            return false;

        return true;
    }

    public void ReturnSlotToInventory(PackingSlotUI slot)
    {
        if (slot == null || slot.CurrentItem == null || InventoryManager.Instance == null)
            return;

        bool added = InventoryManager.Instance.AddItem(slot.CurrentItem);
        if (!added)
        {
            SetError("Inventory is full.");
            return;
        }

        slot.ClearVisualOnly();
        ClearError();
    }

    private void OnPackClicked()
    {
        if (itemSlot == null || boxSlot == null || resultSlot == null)
        {
            SetError("Packing slots not assigned.");
            return;
        }

        if (closedBoxItem == null)
        {
            SetError("Closed box item not assigned.");
            return;
        }

        if (resultSlot.CurrentItem != null)
        {
            SetError("Take the packed box first.");
            return;
        }

        if (itemSlot.CurrentItem == null)
        {
            SetError("Place an item in the Item slot.");
            return;
        }

        if (boxSlot.CurrentItem == null)
        {
            SetError("Place an open box in the Box slot.");
            return;
        }

        if (boxSlot.CurrentItem.itemKey != "box_open")
        {
            SetError("Box slot needs box_open.");
            return;
        }

        resultSlot.SetResultItem(closedBoxItem);
        itemSlot.ClearVisualOnly();
        boxSlot.ClearVisualOnly();
        ClearError();
    }

    private void ReturnInputsToInventory()
    {
        if (itemSlot != null && itemSlot.CurrentItem != null)
            ReturnSlotToInventory(itemSlot);

        if (boxSlot != null && boxSlot.CurrentItem != null)
            ReturnSlotToInventory(boxSlot);
    }

    private void ClearAllSlots()
    {
        if (itemSlot != null)
            itemSlot.ClearVisualOnly();

        if (boxSlot != null)
            boxSlot.ClearVisualOnly();

        if (resultSlot != null)
            resultSlot.ClearVisualOnly();
    }

    private void SetError(string message)
    {
        if (errorText != null)
            errorText.text = message;
    }

    private void ClearError()
    {
        SetError(string.Empty);
    }
}