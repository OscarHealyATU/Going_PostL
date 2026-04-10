using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerminalManagerUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text messageText;

    [Header("Main Pages")]
    [SerializeField] private GameObject upgradesPage;
    [SerializeField] private GameObject vehiclesPage;
    [SerializeField] private GameObject employeesPage;
    [SerializeField] private GameObject propertiesPage;

    [Header("Left Menu Buttons")]
    [SerializeField] private Button upgradesButton;
    [SerializeField] private Button vehiclesButton;
    [SerializeField] private Button employeesButton;
    [SerializeField] private Button propertiesButton;

    [Header("Vehicle Subpages")]
    [SerializeField] private GameObject ownedVehiclesPage;
    [SerializeField] private GameObject forSaleVehiclesPage;

    [Header("Vehicle Page Scripts")]
    [SerializeField] private OwnedVehiclesPageUI ownedVehiclesPageUI;

    [Header("Vehicle Subpage Buttons")]
    [SerializeField] private Button ownedVehiclesButton;
    [SerializeField] private Button forSaleVehiclesButton;

    [Header("Vehicle Purchase Target")]
    [SerializeField] private string purchasedVehicleSpawnScene = "Main";

    [Header("Button Colors")]
    [SerializeField] private Color activeButtonColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color inactiveButtonColor = Color.white;

    [Header("Vehicle Sale Cards")]
    [SerializeField] private VehicleForSaleCardUI[] forSaleCards = new VehicleForSaleCardUI[4];

    private void OnEnable()
    {
        ShowUpgradesPage();
        RefreshTopBar();
        BuildVehicleForSalePage();
        ClearMessage();

        if (ownedVehiclesPageUI != null)
            ownedVehiclesPageUI.Rebuild();
    }

    public void ShowUpgradesPage()
    {
        SetActiveMainPage(upgradesPage);
        UpdateLeftMenuVisuals(upgradesButton);
    }

    public void ShowVehiclesPage()
    {
        SetActiveMainPage(vehiclesPage);
        ShowOwnedVehiclesSubpage();
        BuildVehicleForSalePage();

        if (ownedVehiclesPageUI != null)
            ownedVehiclesPageUI.Rebuild();

        UpdateLeftMenuVisuals(vehiclesButton);
    }

    public void ShowEmployeesPage()
    {
        SetActiveMainPage(employeesPage);
        UpdateLeftMenuVisuals(employeesButton);
    }

    public void ShowPropertiesPage()
    {
        SetActiveMainPage(propertiesPage);
        UpdateLeftMenuVisuals(propertiesButton);
    }

    public void ShowOwnedVehiclesSubpage()
    {
        if (ownedVehiclesPage != null) ownedVehiclesPage.SetActive(true);
        if (forSaleVehiclesPage != null) forSaleVehiclesPage.SetActive(false);

        if (ownedVehiclesPageUI != null)
            ownedVehiclesPageUI.Rebuild();

        UpdateVehicleSubpageVisuals(ownedVehiclesButton);
    }

    public void ShowVehiclesForSaleSubpage()
    {
        if (ownedVehiclesPage != null) ownedVehiclesPage.SetActive(false);
        if (forSaleVehiclesPage != null) forSaleVehiclesPage.SetActive(true);

        BuildVehicleForSalePage();
        UpdateVehicleSubpageVisuals(forSaleVehiclesButton);
    }

    private void SetActiveMainPage(GameObject activePage)
    {
        if (upgradesPage != null) upgradesPage.SetActive(activePage == upgradesPage);
        if (vehiclesPage != null) vehiclesPage.SetActive(activePage == vehiclesPage);
        if (employeesPage != null) employeesPage.SetActive(activePage == employeesPage);
        if (propertiesPage != null) propertiesPage.SetActive(activePage == propertiesPage);

        RefreshTopBar();
    }

    private void UpdateLeftMenuVisuals(Button activeButton)
    {
        SetButtonColor(upgradesButton, activeButton == upgradesButton ? activeButtonColor : inactiveButtonColor);
        SetButtonColor(vehiclesButton, activeButton == vehiclesButton ? activeButtonColor : inactiveButtonColor);
        SetButtonColor(employeesButton, activeButton == employeesButton ? activeButtonColor : inactiveButtonColor);
        SetButtonColor(propertiesButton, activeButton == propertiesButton ? activeButtonColor : inactiveButtonColor);
    }

    private void UpdateVehicleSubpageVisuals(Button activeButton)
    {
        SetButtonColor(ownedVehiclesButton, activeButton == ownedVehiclesButton ? activeButtonColor : inactiveButtonColor);
        SetButtonColor(forSaleVehiclesButton, activeButton == forSaleVehiclesButton ? activeButtonColor : inactiveButtonColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private void BuildVehicleForSalePage()
    {
        VehicleType[] allTypes = VehicleService.GetAllVehicleTypes();

        for (int i = 0; i < forSaleCards.Length; i++)
        {
            if (forSaleCards[i] == null)
                continue;

            if (i < allTypes.Length)
            {
                forSaleCards[i].gameObject.SetActive(true);
                forSaleCards[i].Bind(allTypes[i], this);
            }
            else
            {
                forSaleCards[i].gameObject.SetActive(false);
            }
        }
    }

    public void TryBuyVehicle(int vehicleTypeId)
    {
        var result = VehicleService.TryPurchaseVehicleForScene(vehicleTypeId, purchasedVehicleSpawnScene);

        RefreshTopBar();
        SetMessage(result.message);

        if (ownedVehiclesPageUI != null)
            ownedVehiclesPageUI.Rebuild();

        PlayerService.RefreshAllUI();
    }

    private void RefreshTopBar()
    {
        if (DbBoot.Instance == null)
            return;

        Player player = PlayerService.Get();

        if (moneyText != null)
            moneyText.text = $"Money: €{player.money:0}";

        if (levelText != null)
            levelText.text = $"Level: {PlayerService.GetLevel(player)}";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void SetStatusMessage(string message)
    {
        SetMessage(message);
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }
}