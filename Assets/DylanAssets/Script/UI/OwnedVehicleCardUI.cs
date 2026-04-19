using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OwnedVehicleCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text vehicleTypeText;
    [SerializeField] private TMP_Text sellInfoText;
    [SerializeField] private TMP_Text storageInfoText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button recoverButton;

    private int vehicleId;
    private Action<string> onSold;

    public void Setup(Vehicle vehicle, VehicleType type, Action<string> soldCallback)
    {
        vehicleId = vehicle.id;
        onSold = soldCallback;

        double sellPrice = VehicleService.GetSellPrice(vehicle.vehicleTypeId);
        int usedStorage = VehicleStorageService.GetUsedSlotCount(vehicle.id);
        int maxStorage = VehicleStorageService.GetCapacity(vehicle.id);

        if (vehicleTypeText != null)
            vehicleTypeText.text = type != null ? type.name : "Unknown Vehicle";

        if (sellInfoText != null)
            sellInfoText.text = $"Sell for 75% of original price";

        if (storageInfoText != null)
            storageInfoText.text = $"Storage used: {usedStorage}/{maxStorage}";

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);

            TMP_Text buttonLabel = sellButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
                buttonLabel.text = $"Sell for €{sellPrice:0}";
        }

        if (recoverButton != null)
        {
            recoverButton.onClick.RemoveAllListeners();
            recoverButton.onClick.AddListener(OnRecoverClicked);

            TMP_Text buttonLabel = recoverButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
                buttonLabel.text = "Recover";
        }
    }

    private void OnSellClicked()
    {
        if (VehicleService.TrySellVehicle(vehicleId, out string message, out double sellPrice))
        {
            if (DayManager.Instance != null)
                DayManager.Instance.RegisterMoneyEarned(sellPrice);

            onSold?.Invoke(message);
        }
        else
        {
            //debug.LogError(message);
            onSold?.Invoke(message);
        }
    }

    private void OnRecoverClicked()
    {
        if (VehicleService.TryRecoverVehicleToAssignedBay(vehicleId, out string message))
        {
            //debug.Log(message);
            onSold?.Invoke(message);
        }
        else
        {
            //debug.LogError(message);
            onSold?.Invoke(message);
        }
    }
}