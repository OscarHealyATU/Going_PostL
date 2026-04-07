using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleForSaleCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text vehicleTypeText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text storageText;
    [SerializeField] private Button buyButton;

    private VehicleType boundVehicleType;
    private TerminalManagerUI owner;

    public void Bind(VehicleType vehicleType, TerminalManagerUI ownerUi)
    {
        boundVehicleType = vehicleType;
        owner = ownerUi;

        if (vehicleTypeText != null)
            vehicleTypeText.text = $"Vehicle type: {vehicleType.name}";

        if (priceText != null)
            priceText.text = $"Price: €{vehicleType.baseCost:0}";

        if (storageText != null)
            storageText.text = $"Storage: {vehicleType.storageCapacity}";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnBuyClicked()
    {
        if (owner == null || boundVehicleType == null)
            return;

        owner.TryBuyVehicle(boundVehicleType.id);
    }
}