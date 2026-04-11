using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VehicleStorageSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;

    private VehicleStorageUI owner;
    private int vehicleId = -1;
    private int slotIndex = -1;
    private StoredDelivery storedDelivery;

    public int SlotIndex => slotIndex;
    public int VehicleId => vehicleId;
    public StoredDelivery StoredDelivery => storedDelivery;

    public void Setup(VehicleStorageUI ownerUi, int currentVehicleId, int currentSlotIndex, StoredDelivery currentStoredDelivery, Sprite sprite)
    {
        owner = ownerUi;
        vehicleId = currentVehicleId;
        slotIndex = currentSlotIndex;
        storedDelivery = currentStoredDelivery;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }

    public void SetEmpty(VehicleStorageUI ownerUi, int currentVehicleId, int currentSlotIndex)
    {
        owner = ownerUi;
        vehicleId = currentVehicleId;
        slotIndex = currentSlotIndex;
        storedDelivery = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (owner == null)
            return;

        owner.OnStorageSlotClicked(this);
    }
}