using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text requiredLevelText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private UpgradeDefinition boundUpgrade;
    private UpgradesPageUI owner;

    public void Bind(UpgradeDefinition upgrade, UpgradesPageUI pageOwner)
    {
        boundUpgrade = upgrade;
        owner = pageOwner;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyPressed);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (boundUpgrade == null)
            return;

        if (titleText != null)
            titleText.text = boundUpgrade.title;

        if (descriptionText != null)
            descriptionText.text = boundUpgrade.description;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            if (requiredLevelText != null)
                requiredLevelText.text = "Required Level: -";

            if (priceText != null)
                priceText.text = "Price: -";

            if (buyButtonText != null)
                buyButtonText.text = "Loading";

            if (buyButton != null)
                buyButton.interactable = false;

            return;
        }

        int requiredLevel = boundUpgrade.requiredLevel;
        int price = boundUpgrade.price;

        if (boundUpgrade.upgradeType == UpgradeType.ZoneLicense)
        {
            var zone = DbBoot.Instance.Db.Find<DeliveryZone>(boundUpgrade.zoneId);
            if (zone != null)
            {
                requiredLevel = zone.requiredLevel;
                price = zone.unlockCost;
            }
        }

        if (requiredLevelText != null)
            requiredLevelText.text = $"Required Level: {requiredLevel}";

        if (priceText != null)
            priceText.text = $"Price: €{price}";

        bool isOwned = IsOwned();
        bool hasLevel = PlayerService.GetLevel() >= requiredLevel;
        bool canAfford = PlayerService.GetMoney() >= price;
        bool canBuyNow = CanBuyNow();

        if (buyButtonText != null)
        {
            if (isOwned)
                buyButtonText.text = "Owned";
            else if (!hasLevel)
                buyButtonText.text = "Locked";
            else if (!canBuyNow)
                buyButtonText.text = "Locked";
            else if (!canAfford)
                buyButtonText.text = "Locked";
            else
                buyButtonText.text = "Buy";
        }

        if (buyButton != null)
            buyButton.interactable = !isOwned && hasLevel && canBuyNow && canAfford;
    }

    private bool IsOwned()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null || boundUpgrade == null)
            return false;

        switch (boundUpgrade.upgradeType)
        {
            case UpgradeType.ZoneLicense:
                return ZoneService.IsZoneUnlocked(boundUpgrade.zoneId);

            case UpgradeType.Storage:
                return PlayerService.GetInventorySlotCount() >= 5;
        }

        return false;
    }

    private bool CanBuyNow()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null || boundUpgrade == null)
            return false;

        switch (boundUpgrade.upgradeType)
        {
            case UpgradeType.ZoneLicense:
                return ZoneService.CanUnlockZoneInSequence(boundUpgrade.zoneId);

            case UpgradeType.Storage:
                return !IsOwned();
        }

        return false;
    }

    private void OnBuyPressed()
    {
        if (boundUpgrade == null)
            return;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return;

        switch (boundUpgrade.upgradeType)
        {
            case UpgradeType.ZoneLicense:
            {
                var result = ZoneService.TryUnlockZone(boundUpgrade.zoneId);
                Debug.Log("[UpgradeCardUI] " + result.message);

                if (result.success && DeliveryGridProvider.Instance != null)
                    DeliveryGridProvider.Instance.RefreshUnlockedZones();

                break;
            }

            case UpgradeType.Storage:
            {
                if (IsOwned())
                {
                    Debug.Log("[UpgradeCardUI] Storage upgrade already owned.");
                    break;
                }

                if (PlayerService.GetLevel() < boundUpgrade.requiredLevel)
                {
                    Debug.Log("[UpgradeCardUI] Player level too low for storage upgrade.");
                    break;
                }

                if (!PlayerService.TrySpendMoney(boundUpgrade.price))
                {
                    Debug.Log("[UpgradeCardUI] Not enough money for storage upgrade.");
                    break;
                }

                PlayerService.SetInventorySlotCount(5);

                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.RefreshCapacityFromPlayer();

                Debug.Log("[UpgradeCardUI] Purchased Delivery Satchel. Inventory expanded to 5 slots.");
                break;
            }
        }

        if (owner != null)
            owner.RefreshAll();

        PlayerService.RefreshAllUI();
    }
}