using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPageUI : MonoBehaviour
{
    [Header("Zone Layout Assets")]
    [SerializeField] private List<DeliveryZoneLayoutAsset> zoneLayouts = new List<DeliveryZoneLayoutAsset>();

    [Header("Form")]
    [SerializeField] private TMP_Dropdown zoneDropdown;
    [SerializeField] private TMP_InputField streetInput;
    [SerializeField] private TMP_InputField avenueInput;

    [Header("UI Text")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text currentTileText;
    [SerializeField] private TMP_Text ownedWarehousesText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Buttons")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button refreshButton;

    [Header("Purchase")]
    [SerializeField] private double warehousePrice = 0.0;

    private void Awake()
    {
        SetupZoneDropdown();
        HookUiEvents();
        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void SetupZoneDropdown()
    {
        if (zoneDropdown == null)
            return;

        zoneDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < zoneLayouts.Count; i++)
        {
            DeliveryZoneLayoutAsset layout = zoneLayouts[i];
            if (layout == null)
                continue;

            options.Add(layout.name);
        }

        zoneDropdown.AddOptions(options);
    }

    private void HookUiEvents()
    {
        if (zoneDropdown != null)
            zoneDropdown.onValueChanged.AddListener(_ => RefreshSelectionPreview());

        if (streetInput != null)
            streetInput.onValueChanged.AddListener(_ => RefreshSelectionPreview());

        if (avenueInput != null)
            avenueInput.onValueChanged.AddListener(_ => RefreshSelectionPreview());

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseClicked);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshAll);
    }

    private void RefreshAll()
    {
        RefreshMoney();
        RefreshSelectionPreview();
        RefreshOwnedWarehouses();
        ClearFeedback();
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        Player player = PlayerService.Get();
        if (player == null)
        {
            moneyText.text = "Money: €0";
            return;
        }

        moneyText.text = $"Money: €{player.money:0}";
    }

    private void RefreshSelectionPreview()
    {
        if (currentTileText == null)
            return;

        int tileX;
        int tileZ;

        if (!TryReadTileInputs(out tileX, out tileZ))
        {
            currentTileText.text = "Current Tile: -";
            return;
        }

        DeliveryZoneLayoutAsset selectedLayout = GetSelectedLayout();
        if (selectedLayout == null)
        {
            currentTileText.text = "Current Tile: -";
            return;
        }

        currentTileText.text = $"Current Tile: {selectedLayout.name} {tileX}, {tileZ}";
    }

    private void RefreshOwnedWarehouses()
    {
        if (ownedWarehousesText == null)
            return;

        List<Warehouse> warehouses = WarehouseService.GetAllOwned();

        if (warehouses.Count == 0)
        {
            ownedWarehousesText.text = "Owned Warehouses: None";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Owned Warehouses:");

        for (int i = 0; i < warehouses.Count; i++)
        {
            Warehouse warehouse = warehouses[i];
            sb.AppendLine($"ID {warehouse.id}: {warehouse.zoneName} {warehouse.tileX}, {warehouse.tileZ}");
        }

        ownedWarehousesText.text = sb.ToString();
    }

    private void OnPurchaseClicked()
    {
        ClearFeedback();

        DeliveryZoneLayoutAsset selectedLayout = GetSelectedLayout();
        if (selectedLayout == null)
        {
            SetFeedback("No zone layout selected.");
            return;
        }

        int tileX;
        int tileZ;
        if (!TryReadTileInputs(out tileX, out tileZ))
        {
            SetFeedback("Enter valid tile coordinates.");
            return;
        }

        if (tileX < 0 || tileZ < 0)
        {
            SetFeedback("Coordinates must be 0 or greater.");
            return;
        }

        if (tileX >= selectedLayout.noOfHousesX || tileZ >= selectedLayout.noOfHousesZ)
        {
            SetFeedback("That tile is outside the selected zone grid.");
            return;
        }

        string zoneName = selectedLayout.name;

        if (WarehouseService.HasWarehouseAtTile(zoneName, tileX, tileZ))
        {
            SetFeedback("A warehouse already exists on that tile.");
            return;
        }

        Vector3 worldPos = TileToWorld(selectedLayout, tileX, tileZ);

        WarehouseService.WarehousePurchaseResult result =
            WarehouseService.TryPurchaseWarehouse(
                zoneName,
                tileX,
                tileZ,
                worldPos.x,
                worldPos.y,
                worldPos.z,
                warehousePrice
            );

        SetFeedback(result.message);

        if (!result.success)
            return;

        RefreshMoney();
        RefreshOwnedWarehouses();
        RefreshSelectionPreview();
    }

    private bool TryReadTileInputs(out int tileX, out int tileZ)
    {
        tileX = -1;
        tileZ = -1;

        if (streetInput == null || avenueInput == null)
            return false;

        bool xOk = int.TryParse(streetInput.text, out tileX);
        bool zOk = int.TryParse(avenueInput.text, out tileZ);

        return xOk && zOk;
    }

    private DeliveryZoneLayoutAsset GetSelectedLayout()
    {
        if (zoneDropdown == null || zoneDropdown.options == null || zoneDropdown.options.Count == 0)
            return null;

        int index = Mathf.Clamp(zoneDropdown.value, 0, zoneLayouts.Count - 1);
        if (index < 0 || index >= zoneLayouts.Count)
            return null;

        return zoneLayouts[index];
    }

    private Vector3 TileToWorld(DeliveryZoneLayoutAsset layout, int tileX, int tileZ)
    {
        return new Vector3(
            layout.xStartPosition + tileX * layout.distance,
            0f,
            layout.zStartPosition + tileZ * layout.distance
        );
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        Debug.Log("[PropertiesPageUI] " + message);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
}