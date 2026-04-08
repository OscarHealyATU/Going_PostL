using System;
using System.Collections.Generic;
using System.Linq;

public static class ZoneService
{
    public struct UnlockResult
    {
        public bool success;
        public string message;
        public DeliveryZone zone;
    }

    public static List<DeliveryZone> GetAllZones()
    {
        if (DbBoot.Instance == null)
            return new List<DeliveryZone>();

        return DbBoot.Instance.Db.Table<DeliveryZone>()
            .OrderBy(z => z.id)
            .ToList();
    }

    public static List<DeliveryZone> GetUnlockedZones()
    {
        if (DbBoot.Instance == null)
            return new List<DeliveryZone>();

        var db = DbBoot.Instance.Db;
        var player = PlayerService.Get();
        if (player == null)
            return new List<DeliveryZone>();

        var unlockedIds = db.Table<PlayerZoneUnlock>()
            .Where(x => x.playerId == player.id)
            .Select(x => x.zoneId)
            .ToList();

        return db.Table<DeliveryZone>()
            .Where(z => unlockedIds.Contains(z.id))
            .OrderBy(z => z.id)
            .ToList();
    }

    public static bool IsZoneUnlocked(int zoneId)
    {
        if (DbBoot.Instance == null)
            return false;

        var db = DbBoot.Instance.Db;
        var player = PlayerService.Get();
        if (player == null)
            return false;

        return db.Table<PlayerZoneUnlock>()
            .Any(x => x.playerId == player.id && x.zoneId == zoneId);
    }

    public static int GetNextLockedZoneId()
    {
        var zones = GetAllZones();
        for (int i = 0; i < zones.Count; i++)
        {
            if (!IsZoneUnlocked(zones[i].id))
                return zones[i].id;
        }

        return -1;
    }

    public static bool CanUnlockZoneInSequence(int zoneId)
    {
        int nextLockedZoneId = GetNextLockedZoneId();
        if (nextLockedZoneId == -1)
            return false;

        return zoneId == nextLockedZoneId;
    }

    public static UnlockResult TryUnlockZone(int zoneId)
    {
        UnlockResult result = new UnlockResult
        {
            success = false,
            message = "Zone unlock failed.",
            zone = null
        };

        if (DbBoot.Instance == null)
        {
            result.message = "Database not available.";
            return result;
        }

        var db = DbBoot.Instance.Db;
        var player = PlayerService.Get();
        if (player == null)
        {
            result.message = "Player not found.";
            return result;
        }

        var zone = db.Find<DeliveryZone>(zoneId);
        if (zone == null)
        {
            result.message = "Zone not found.";
            return result;
        }

        result.zone = zone;

        if (IsZoneUnlocked(zoneId))
        {
            result.message = $"{zone.name} is already unlocked.";
            return result;
        }

        int expectedZoneId = GetNextLockedZoneId();
        if (expectedZoneId != -1 && zoneId != expectedZoneId)
        {
            var requiredPreviousZone = db.Find<DeliveryZone>(expectedZoneId);
            if (requiredPreviousZone != null)
                result.message = $"You must unlock {requiredPreviousZone.name} first.";
            else
                result.message = "You must unlock the previous zone first.";

            return result;
        }

        int playerLevel = PlayerService.GetLevel(player);
        if (playerLevel < zone.requiredLevel)
        {
            result.message = $"{zone.name} requires level {zone.requiredLevel}.";
            return result;
        }

        if (player.money < zone.unlockCost)
        {
            result.message = $"Not enough money to unlock {zone.name}.";
            return result;
        }

        db.RunInTransaction(() =>
        {
            player.money -= zone.unlockCost;
            db.Update(player);

            db.Insert(new PlayerZoneUnlock
            {
                playerId = player.id,
                zoneId = zone.id,
                unlockedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            db.Insert(new TransactionLog
            {
                playerId = player.id,
                type = "zone_unlock",
                amount = -zone.unlockCost,
                description = $"Unlocked {zone.name}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
        });

        if (DayManager.Instance != null)
            DayManager.Instance.RegisterMoneySpent(zone.unlockCost);

        result.success = true;
        result.message = $"{zone.name} unlocked.";
        return result;
    }
}