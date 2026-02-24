using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleShopUI : MonoBehaviour
{
    [Header("Vehicle List")]
    public Transform vehicleListContent;   // parent where rows spawn
    public Button vehicleRowPrefab;        // prefab button

    [Header("Buttons")]
    public Button buyButton;
    public Button[] bayButtons = new Button[4];

    [Header("World Spawn Target")]
    [Tooltip("Must match the Main scene name exactly (Build Settings).")]
    public string mainWorldSceneName = "Main";

    [Header("Text (TMP)")]
    public TMP_Text moneyText;
    public TMP_Text selectedText;
    public TMP_Text errorText;

    private int? _selectedVehicleTypeId = null;
    private int? _selectedBayIndex = null;

    private bool _wired = false;

    void OnEnable()
    {
        Rebuild();
    }

    void Start()
    {
        HookButtons();
        _wired = true;

        RefreshUI();
    }

    void Update()
    {
        RefreshMoney();

        // Optional: keep bay availability live-updating while the panel is open
        RefreshBayAvailability();
        RefreshBuyInteractable();
    }

    /// <summary>
    /// Re-reads vehicle types and rebuilds the list UI.
    /// Safe to call multiple times.
    /// </summary>
    public void Rebuild()
    {
        ClearError();

        if (DbBoot.Instance == null)
        {
            SetError("DB not initialised (DbBoot.Instance is null).");
            return;
        }

        VehicleTypeStore.LoadOrSeedDefaults(DbBoot.Instance.Db);

        BuildVehicleList();

        if (_wired)
            RefreshUI();
    }

    private void BuildVehicleList()
    {
        if (vehicleListContent == null || vehicleRowPrefab == null)
        {
            SetError("Vehicle list is not wired (Content/Prefab missing).");
            return;
        }

        for (int i = vehicleListContent.childCount - 1; i >= 0; i--)
            Destroy(vehicleListContent.GetChild(i).gameObject);

        foreach (var vt in VehicleTypeStore.All.OrderBy(v => v.baseCost))
        {
            var row = Instantiate(vehicleRowPrefab, vehicleListContent);

            var label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = $"{vt.name}  (€{vt.baseCost:0})";

            int idCopy = vt.id;
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() =>
            {
                _selectedVehicleTypeId = idCopy;
                ClearError();
                RefreshUI();
            });
        }

        if (VehicleTypeStore.All.Count == 0)
            SetError("No vehicle types found.");
    }

    private void HookButtons()
    {
        if (bayButtons == null || bayButtons.Length != 4)
        {
            SetError("Bay buttons array must be size 4.");
            return;
        }

        for (int i = 0; i < bayButtons.Length; i++)
        {
            int idx = i;
            if (bayButtons[i] == null) continue;

            bayButtons[i].onClick.RemoveAllListeners();
            bayButtons[i].onClick.AddListener(() =>
            {
                // Don’t allow selecting an occupied bay
                if (IsBayOccupied(idx))
                {
                    SetError($"Bay {idx + 1} is already occupied.");
                    _selectedBayIndex = null;
                    RefreshUI();
                    return;
                }

                _selectedBayIndex = idx;
                ClearError();
                RefreshUI();
            });
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnBuyClicked()
    {
        ClearError();

        if (_selectedVehicleTypeId == null)
        {
            SetError("Select a vehicle first.");
            return;
        }

        if (_selectedBayIndex == null)
        {
            SetError("Select a bay (1–4).");
            return;
        }

        // Safety: avoid accidentally writing MainWorld again (remove later if you want)
        if (!string.Equals(mainWorldSceneName?.Trim(), "Main", StringComparison.Ordinal))
        {
            SetError($"Spawn scene must be 'Main' (currently '{mainWorldSceneName}').");
            return;
        }

        // Final bay occupied check right before purchase
        if (IsBayOccupied(_selectedBayIndex.Value))
        {
            SetError($"Bay {_selectedBayIndex.Value + 1} is already occupied.");
            _selectedBayIndex = null;
            RefreshUI();
            return;
        }

        try
        {
            VehicleService.BuyVehicleQueuedForWorld(
                _selectedVehicleTypeId.Value,
                spawnScene: mainWorldSceneName.Trim(),
                spawnBay0Based: _selectedBayIndex.Value
            );

            SetError($"Purchased! Will spawn in {mainWorldSceneName}, Bay {_selectedBayIndex.Value + 1}.");
            RefreshUI();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private void RefreshUI()
    {
        RefreshMoney();
        RefreshBayAvailability();

        string vehicleName = null;
        if (_selectedVehicleTypeId != null)
        {
            var vt = VehicleTypeStore.All.FirstOrDefault(v => v.id == _selectedVehicleTypeId.Value);
            vehicleName = vt?.name;
        }

        // If selected bay became occupied, clear selection
        if (_selectedBayIndex != null && IsBayOccupied(_selectedBayIndex.Value))
            _selectedBayIndex = null;

        var bayText = _selectedBayIndex != null
            ? $"Bay {_selectedBayIndex.Value + 1}"
            : "No bay";

        if (selectedText != null)
            selectedText.text = $"Selected: {(vehicleName ?? "No vehicle")} | {bayText}";

        RefreshBuyInteractable();
    }

    private void RefreshBuyInteractable()
    {
        if (buyButton == null) return;

        bool hasVehicle = _selectedVehicleTypeId != null;
        bool hasBay = _selectedBayIndex != null;
        bool bayFree = hasBay && !IsBayOccupied(_selectedBayIndex.Value);

        buyButton.interactable = hasVehicle && hasBay && bayFree;
    }

    private void RefreshBayAvailability()
    {
        if (bayButtons == null) return;

        for (int i = 0; i < bayButtons.Length; i++)
        {
            if (bayButtons[i] == null) continue;

            bool occupied = IsBayOccupied(i);
            bayButtons[i].interactable = !occupied;
        }
    }

    private bool IsBayOccupied(int bay0Based)
    {
        return VehicleService.IsBayOccupied(mainWorldSceneName.Trim(), bay0Based);
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;

        var player = PlayerService.Get();
        moneyText.text = $"Money: €{player.money:0}";
    }

    private void SetError(string msg)
    {
        if (errorText != null)
            errorText.text = msg;

        Debug.LogWarning("VehicleShopUI: " + msg);
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }
}