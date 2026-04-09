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
    private TMP_Text buyButtonText;

    private void Awake()
    {
        if (buyButton != null)
            buyButtonText = buyButton.GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        PlayerService.OnMoneyChanged += OnMoneyChanged;
    }

    private void OnDisable()
    {
        PlayerService.OnMoneyChanged -= OnMoneyChanged;
    }

    public void Bind(VehicleType vehicleType, TerminalManagerUI ownerUi)
    {
        boundVehicleType = vehicleType;
        owner = ownerUi;

        if (vehicleTypeText != null)
            vehicleTypeText.text = vehicleType.name;

        if (priceText != null)
            priceText.text = $"Price: €{vehicleType.baseCost:0}";

        if (storageText != null)
            storageText.text = $"Storage: {vehicleType.storageCapacity}";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        RefreshBuyButton();
    }

    private void OnMoneyChanged(double _)
    {
        RefreshBuyButton();
    }

    private void RefreshBuyButton()
    {
        if (buyButton == null || boundVehicleType == null)
            return;

        double playerMoney = PlayerService.Get().money;
        bool canAfford = playerMoney >= boundVehicleType.baseCost;

        buyButton.interactable = canAfford;

        if (buyButtonText != null)
            buyButtonText.text = canAfford ? "Buy" : "Locked";
    }

    private void OnBuyClicked()
    {
        if (owner == null || boundVehicleType == null)
            return;

        owner.TryBuyVehicle(boundVehicleType.id);
    }
}