using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class DbBoot : MonoBehaviour
{
    public static DbBoot Instance { get; private set; }
    public GameDb GameDb { get; private set; }

    public SQLite.SQLiteConnection Db => GameDb != null ? GameDb.Db : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameDb = new GameDb();
        Debug.Log("DB path: " + GameDb.DbPath);

        EnsurePlayerSchema();
        EnsurePlayerExists();
        EnsureDeliveryZonesSeeded();
        EnsureStartingZonesUnlocked();

        VehicleTypeStore.LoadOrSeedDefaults(Db);
        Debug.Log("[DbBoot] VehicleType rows now: " + Db.Table<VehicleType>().Count());
    }

    private void EnsurePlayerSchema()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsurePlayerSchema.");
            return;
        }

        try
        {
            Db.Execute("ALTER TABLE Player ADD COLUMN inventorySlotCount INTEGER NOT NULL DEFAULT 3");
            Debug.Log("[DbBoot] Added inventorySlotCount column to Player");
        }
        catch (Exception)
        {
            // Column already exists or Player table not yet created in a fresh DB.
        }

        try
        {
            var players = Db.Table<Player>().ToList();

            bool anyUpdated = false;
            foreach (var player in players)
            {
                if (player.inventorySlotCount <= 0)
                {
                    player.inventorySlotCount = 3;
                    Db.Update(player);
                    anyUpdated = true;
                }
            }

            if (anyUpdated)
                Debug.Log("[DbBoot] Backfilled inventorySlotCount for existing players");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DbBoot] EnsurePlayerSchema backfill skipped: " + ex.Message);
        }
    }

    private void EnsurePlayerExists()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsurePlayerExists.");
            return;
        }

        var player = Db.Table<Player>().FirstOrDefault();

        if (player == null)
        {
            Db.Insert(new Player
            {
                name = "Player",
                money = 0.0,
                totalExperience = 0,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                inventorySlotCount = 3,

                returnValid = 0,
                returnX = 0f,
                returnY = 0f,
                returnZ = 0f,
                returnYaw = 0f,

                hasResumePoint = 0,
                savedScene = null,
                savedX = 0f,
                savedY = 0f,
                savedZ = 0f,
                savedYaw = 0f
            });

            Debug.Log("[DbBoot] Created Player row");
        }
        else
        {
            if (player.inventorySlotCount <= 0)
            {
                player.inventorySlotCount = 3;
                Db.Update(player);
                Debug.Log("[DbBoot] Fixed Player inventorySlotCount to default 3");
            }

            Debug.Log("[DbBoot] Player exists id=" + player.id);
        }
    }

    private void EnsureDeliveryZonesSeeded()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureDeliveryZonesSeeded.");
            return;
        }

        List<DeliveryZone> desiredZones = GetDesiredDeliveryZones();

        foreach (var desiredZone in desiredZones)
        {
            var existingZone = Db.Find<DeliveryZone>(desiredZone.id);

            if (existingZone == null)
            {
                Db.Insert(desiredZone);
                Debug.Log($"[DbBoot] Inserted DeliveryZone '{desiredZone.name}'");
            }
            else
            {
                existingZone.name = desiredZone.name;
                existingZone.unlockCost = desiredZone.unlockCost;
                existingZone.requiredLevel = desiredZone.requiredLevel;
                existingZone.payMultiplier = desiredZone.payMultiplier;
                existingZone.xpMultiplier = desiredZone.xpMultiplier;
                existingZone.startsUnlocked = desiredZone.startsUnlocked;

                Db.Update(existingZone);
                Debug.Log($"[DbBoot] Updated DeliveryZone '{existingZone.name}'");
            }
        }

        Debug.Log("[DbBoot] DeliveryZone rows now: " + Db.Table<DeliveryZone>().Count());
    }

    private List<DeliveryZone> GetDesiredDeliveryZones()
    {
        return new List<DeliveryZone>
        {
            new DeliveryZone
            {
                id = 1,
                name = "Zone 1",
                unlockCost = 0,
                requiredLevel = 1,
                payMultiplier = 1.0f,
                xpMultiplier = 1.0f,
                startsUnlocked = 1
            },
            new DeliveryZone
            {
                id = 2,
                name = "Zone 2",
                unlockCost = 2500,
                requiredLevel = 3,
                payMultiplier = 1.2f,
                xpMultiplier = 1.2f,
                startsUnlocked = 0
            },
            new DeliveryZone
            {
                id = 3,
                name = "Zone 3",
                unlockCost = 6000,
                requiredLevel = 6,
                payMultiplier = 1.6f,
                xpMultiplier = 1.3f,
                startsUnlocked = 0
            },
            new DeliveryZone
            {
                id = 4,
                name = "Zone 4",
                unlockCost = 11000,
                requiredLevel = 10,
                payMultiplier = 2.0f,
                xpMultiplier = 1.5f,
                startsUnlocked = 0
            }
        };
    }

    private void EnsureStartingZonesUnlocked()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureStartingZonesUnlocked.");
            return;
        }

        var player = Db.Table<Player>().FirstOrDefault();
        if (player == null)
            return;

        var startingZones = Db.Table<DeliveryZone>()
            .Where(z => z.startsUnlocked == 1)
            .ToList();

        foreach (var zone in startingZones)
        {
            bool alreadyUnlocked = Db.Table<PlayerZoneUnlock>()
                .Any(x => x.playerId == player.id && x.zoneId == zone.id);

            if (alreadyUnlocked)
                continue;

            Db.Insert(new PlayerZoneUnlock
            {
                playerId = player.id,
                zoneId = zone.id,
                unlockedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            Debug.Log($"[DbBoot] Auto-unlocked starting zone '{zone.name}'");
        }
    }

    private void OnApplicationQuit()
    {
        DisposeDb();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            DisposeDb();
    }

    private void DisposeDb()
    {
        GameDb?.Dispose();
        GameDb = null;

        if (Instance == this)
            Instance = null;
    }
}