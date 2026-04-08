using UnityEngine;

public enum UpgradeType
{
    ZoneLicense
}

[System.Serializable]
public class UpgradeDefinition
{
    public UpgradeType upgradeType = UpgradeType.ZoneLicense;

    [Header("Display")]
    public string title;
    [TextArea(2, 4)] public string description;

    [Header("Requirements")]
    public int requiredLevel = 1;
    public int price = 0;

    [Header("Zone License")]
    public int zoneId = 1;
}