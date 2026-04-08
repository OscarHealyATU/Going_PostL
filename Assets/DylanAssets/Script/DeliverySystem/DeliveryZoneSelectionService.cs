using System.Collections.Generic;
using UnityEngine;

public static class DeliveryZoneSelectionService
{
    public static List<int> GetAvailableZoneIds(int maxZoneId)
    {
        List<int> result = new List<int>();

        for (int zoneId = 1; zoneId <= maxZoneId; zoneId++)
        {
            if (IsZoneAvailable(zoneId))
                result.Add(zoneId);
        }

        return result;
    }

    public static bool IsZoneAvailable(int zoneId)
    {
        if (zoneId <= 1)
            return true;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        return ZoneService.IsZoneUnlocked(zoneId);
    }

    public static int GetRandomAvailableZoneId(int maxZoneId)
    {
        List<int> available = GetAvailableZoneIds(maxZoneId);

        if (available.Count == 0)
            return 1;

        return available[Random.Range(0, available.Count)];
    }
}