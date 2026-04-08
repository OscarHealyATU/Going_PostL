using UnityEngine;

public static class DeliveryRewardService
{
    public static int GetFinalPay(int basePay, int zoneId)
    {
        if (DbBoot.Instance == null)
            return basePay;

        var zone = DbBoot.Instance.Db.Find<DeliveryZone>(zoneId);
        if (zone == null)
            return basePay;

        return Mathf.RoundToInt(basePay * zone.payMultiplier);
    }

    public static int GetFinalXp(int baseXp, int zoneId)
    {
        if (DbBoot.Instance == null)
            return baseXp;

        var zone = DbBoot.Instance.Db.Find<DeliveryZone>(zoneId);
        if (zone == null)
            return baseXp;

        return Mathf.RoundToInt(baseXp * zone.xpMultiplier);
    }
}