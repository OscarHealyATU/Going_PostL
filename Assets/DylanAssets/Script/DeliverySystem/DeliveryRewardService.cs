using UnityEngine;

public static class DeliveryRewardService
{
    public static int GetFinalPay(int basePay, int zoneId)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return basePay;

        var zone = DbBoot.Instance.Db.Find<DeliveryZone>(zoneId);
        if (zone == null)
            return basePay;

        return Mathf.RoundToInt(basePay * zone.payMultiplier);
    }

    public static int GetFinalXp(int baseXp, int zoneId)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return baseXp;

        var zone = DbBoot.Instance.Db.Find<DeliveryZone>(zoneId);
        if (zone == null)
            return baseXp;

        return Mathf.RoundToInt(baseXp * zone.xpMultiplier);
    }

    public static bool TryGetZone(int zoneId, out DeliveryZone zone)
    {
        zone = null;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        zone = DbBoot.Instance.Db.Find<DeliveryZone>(zoneId);
        return zone != null;
    }
}