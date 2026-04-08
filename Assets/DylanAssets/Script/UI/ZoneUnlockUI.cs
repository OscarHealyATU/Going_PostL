using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneUnlockUI : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private int zoneId = 2;

    [Header("UI")]
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button unlockButton;

    private void OnEnable()
    {
        Refresh();
        PlayerService.OnMoneyChanged += OnPlayerStateChanged;
        PlayerService.OnExperienceChanged += OnExperienceChanged;
    }

    private void OnDisable()
    {
        PlayerService.OnMoneyChanged -= OnPlayerStateChanged;
        PlayerService.OnExperienceChanged -= OnExperienceChanged;
    }

    private void OnPlayerStateChanged(double _)
    {
        Refresh();
    }

    private void OnExperienceChanged(int level, int expIntoLevel, int expNeeded)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (DbBoot.Instance == null)
            return;

        var db = DbBoot.Instance.Db;
        var zone = db.Find<DeliveryZone>(zoneId);

        if (zone == null)
        {
            if (zoneNameText != null) zoneNameText.text = $"Zone {zoneId}";
            if (requirementText != null) requirementText.text = "Zone data missing";
            if (rewardText != null) rewardText.text = "";
            if (statusText != null) statusText.text = "Unavailable";
            if (unlockButton != null) unlockButton.interactable = false;
            return;
        }

        int playerLevel = PlayerService.GetLevel();
        double playerMoney = PlayerService.GetMoney();
        bool unlocked = ZoneService.IsZoneUnlocked(zone.id);
        bool inSequence = ZoneService.CanUnlockZoneInSequence(zone.id);

        if (zoneNameText != null)
            zoneNameText.text = zone.name;

        if (requirementText != null)
            requirementText.text = $"Cost: €{zone.unlockCost}\nRequires Level {zone.requiredLevel}";

        if (rewardText != null)
            rewardText.text = $"Pay x{zone.payMultiplier:0.00}  |  XP x{zone.xpMultiplier:0.00}";

        if (statusText != null)
        {
            if (unlocked)
                statusText.text = "Unlocked";
            else if (!inSequence)
                statusText.text = "Unlock previous zone first";
            else if (playerLevel < zone.requiredLevel)
                statusText.text = $"Need Level {zone.requiredLevel}";
            else if (playerMoney < zone.unlockCost)
                statusText.text = "Not enough money";
            else
                statusText.text = "Ready to unlock";
        }

        if (unlockButton != null)
            unlockButton.interactable = !unlocked
                                        && inSequence
                                        && playerLevel >= zone.requiredLevel
                                        && playerMoney >= zone.unlockCost;
    }

    public void OnUnlockPressed()
    {
        var result = ZoneService.TryUnlockZone(zoneId);
        Debug.Log("[ZoneUnlockUI] " + result.message);

        RefreshAllZonePanels();
        PlayerService.RefreshAllUI();
    }

    private void RefreshAllZonePanels()
    {
        var allPanels = FindObjectsByType<ZoneUnlockUI>(FindObjectsSortMode.None);
        for (int i = 0; i < allPanels.Length; i++)
            allPanels[i].Refresh();
    }
}