using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UpgradesPageUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;

    [Header("Layout")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UpgradeCardUI cardPrefab;

    [Header("Upgrade Data")]
    [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    private readonly List<UpgradeCardUI> spawnedCards = new List<UpgradeCardUI>();

    private void OnEnable()
    {
        Rebuild();
        RefreshAll();

        PlayerService.OnMoneyChanged += OnMoneyChanged;
        PlayerService.OnExperienceChanged += OnExperienceChanged;
    }

    private void OnDisable()
    {
        PlayerService.OnMoneyChanged -= OnMoneyChanged;
        PlayerService.OnExperienceChanged -= OnExperienceChanged;
    }

    private void OnMoneyChanged(double _)
    {
        RefreshAll();
    }

    private void OnExperienceChanged(int level, int expIntoLevel, int expNeeded)
    {
        RefreshAll();
    }

    public void Rebuild()
    {
        if (contentRoot == null || cardPrefab == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        spawnedCards.Clear();

        List<UpgradeDefinition> sortedUpgrades = upgrades
            .OrderBy(u => GetSortRequiredLevel(u))
            .ThenBy(u => GetSortPrice(u))
            .ToList();

        for (int i = 0; i < sortedUpgrades.Count; i++)
        {
            UpgradeCardUI card = Instantiate(cardPrefab, contentRoot);
            card.name = $"UpgradeCard_{i + 1}";
            card.Bind(sortedUpgrades[i], this);
            spawnedCards.Add(card);
        }
    }

    private int GetSortRequiredLevel(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return int.MaxValue;

        if (upgrade.upgradeType == UpgradeType.ZoneLicense && DbBoot.Instance != null && DbBoot.Instance.Db != null)
        {
            var zone = DbBoot.Instance.Db.Find<DeliveryZone>(upgrade.zoneId);
            if (zone != null)
                return zone.requiredLevel;
        }

        return upgrade.requiredLevel;
    }

    private int GetSortPrice(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return int.MaxValue;

        if (upgrade.upgradeType == UpgradeType.ZoneLicense && DbBoot.Instance != null && DbBoot.Instance.Db != null)
        {
            var zone = DbBoot.Instance.Db.Find<DeliveryZone>(upgrade.zoneId);
            if (zone != null)
                return zone.unlockCost;
        }

        return upgrade.price;
    }

    public void RefreshAll()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            if (moneyText != null)
                moneyText.text = "Money: €0";

            if (levelText != null)
                levelText.text = "Level: 1";

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    spawnedCards[i].Refresh();
            }

            return;
        }

        if (moneyText != null)
            moneyText.text = $"Money: €{PlayerService.GetMoney():0}";

        if (levelText != null)
            levelText.text = $"Level: {PlayerService.GetLevel()}";

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                spawnedCards[i].Refresh();
        }
    }
}