using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OwnedVehicleCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text vehicleTypeText;
    [SerializeField] private TMP_Text sellInfoText;
    [SerializeField] private Button sellButton;

    private int vehicleId;
    private Action<string> onSold;

    public void Setup(Vehicle vehicle, VehicleType type, Action<string> soldCallback)
    {
        vehicleId = vehicle.id;
        onSold = soldCallback;

        double sellPrice = VehicleService.GetSellPrice(vehicle.vehicleTypeId);

        if (vehicleTypeText != null)
            vehicleTypeText.text = type != null ? type.name : "Unknown Vehicle";

        if (sellInfoText != null)
            sellInfoText.text = $"Sell for €{sellPrice:0} (75% of original price)";

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);

            TMP_Text buttonLabel = sellButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
                buttonLabel.text = $"Sell for €{sellPrice:0}";
        }
    }

    private void OnSellClicked()
    {
        if (VehicleService.TrySellVehicle(vehicleId, out string message, out double sellPrice))
        {
            onSold?.Invoke(message);
        }
        else
        {
            Debug.LogError(message);
            onSold?.Invoke(message);
        }
    }
}