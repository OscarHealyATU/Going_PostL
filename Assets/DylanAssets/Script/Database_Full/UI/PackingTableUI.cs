using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackingTableUI : MonoBehaviour
{
    [Header("Slots")]
    public PackingSlotUI itemSlot;
    public PackingSlotUI boxSlot;
    public PackingSlotUI resultSlot;

    [Header("Packing Result")]
    public ItemData closedBoxItem;

    [Header("Buttons")]
    public Button packButton;
    public Button closeButton;

    [Header("Text")]
    public TMP_Text errorText;

    void Start()
    {
        if (packButton != null)
            packButton.onClick.AddListener(OnPackClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        RefreshUI();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool validItem = itemSlot != null && itemSlot.HasItem;
        bool validBox = boxSlot != null && boxSlot.HasItem && boxSlot.Item != null && boxSlot.Item.itemKey == "open_box";
        bool resultEmpty = resultSlot != null && !resultSlot.HasItem;

        bool canPack = validItem && validBox && resultEmpty && closedBoxItem != null;

        if (packButton != null)
            packButton.interactable = canPack;

        if (errorText != null)
            errorText.text = "";
    }

    private void OnPackClicked()
    {
        if (itemSlot == null || boxSlot == null || resultSlot == null)
        {
            SetError("Packing UI is not set up correctly.");
            return;
        }

        if (!itemSlot.HasItem)
        {
            SetError("Place an item first.");
            return;
        }

        if (!boxSlot.HasItem)
        {
            SetError("Place a box first.");
            return;
        }

        if (boxSlot.Item == null || boxSlot.Item.itemKey != "open_box")
        {
            SetError("That is not a valid box.");
            return;
        }

        if (resultSlot.HasItem)
        {
            SetError("Remove the result first.");
            return;
        }

        ItemData packedResult = PackingService.TryPack(itemSlot.Item, boxSlot.Item, closedBoxItem);

        if (packedResult == null)
        {
            SetError("Packing failed.");
            return;
        }

        itemSlot.ClearSlot();
        boxSlot.ClearSlot();
        resultSlot.SetItem(packedResult);

        RefreshUI();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void SetError(string msg)
    {
        if (errorText != null)
            errorText.text = msg;

        Debug.LogWarning("PackingTableUI: " + msg);
    }
}