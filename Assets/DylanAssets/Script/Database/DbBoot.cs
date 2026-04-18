using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class DbBoot : MonoBehaviour
{
    public static DbBoot Instance { get; private set; }
    public GameDb GameDb { get; private set; }

    public SQLite.SQLiteConnection Db => GameDb != null ? GameDb.Db : null;

    [Header("Starter Warehouse")]
    [SerializeField] private string starterWarehouseZoneName = "Zone 1(1)";
    [SerializeField] private int starterWarehouseTileX = 4;
    [SerializeField] private int starterWarehouseTileZ = 6;
    [SerializeField] private float starterWarehouseWorldX = 220f;
    [SerializeField] private float starterWarehouseWorldY = 0f;
    [SerializeField] private float starterWarehouseWorldZ = 770f;

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
        EnsureWarehouseSchema();
        EnsureStarterWarehouseRegisteredAtBoot();
        EnsureVehicleSchema();

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
                money = 100000,
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

    private void EnsureWarehouseSchema()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureWarehouseSchema.");
            return;
        }

        try
        {
            Db.CreateTable<Warehouse>();
            Debug.Log("[DbBoot] Ensured Warehouse table exists");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DbBoot] EnsureWarehouseSchema failed: " + ex.Message);
        }
    }

    private void EnsureStarterWarehouseRegisteredAtBoot()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureStarterWarehouseRegisteredAtBoot.");
            return;
        }

        if (string.IsNullOrWhiteSpace(starterWarehouseZoneName))
        {
            Debug.LogWarning("[DbBoot] Starter warehouse zone name is empty.");
            return;
        }

        var player = Db.Table<Player>().FirstOrDefault();
        if (player == null)
        {
            Debug.LogWarning("[DbBoot] No player found when trying to create starter warehouse at boot.");
            return;
        }

        var existingStarter = Db.Table<Warehouse>()
            .FirstOrDefault(w => w.playerId == player.id && w.isStarterWarehouse == 1);

        if (existingStarter != null)
            return;

        var sameTile = Db.Table<Warehouse>()
            .FirstOrDefault(w =>
                w.playerId == player.id &&
                w.zoneName == starterWarehouseZoneName &&
                w.tileX == starterWarehouseTileX &&
                w.tileZ == starterWarehouseTileZ);

        if (sameTile != null)
            return;

        Db.Insert(new Warehouse
        {
            playerId = player.id,
            zoneName = starterWarehouseZoneName,
            tileX = starterWarehouseTileX,
            tileZ = starterWarehouseTileZ,
            worldX = starterWarehouseWorldX,
            worldY = starterWarehouseWorldY,
            worldZ = starterWarehouseWorldZ,
            isStarterWarehouse = 1,
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });

        Debug.Log($"[DbBoot] Registered starter warehouse at boot in '{starterWarehouseZoneName}' at tile ({starterWarehouseTileX}, {starterWarehouseTileZ})");
    }

    public void EnsureStarterWarehouseExists(
        string zoneName,
        int tileX,
        int tileZ,
        float worldX,
        float worldY,
        float worldZ)
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureStarterWarehouseExists.");
            return;
        }

        if (string.IsNullOrWhiteSpace(zoneName))
        {
            Debug.LogWarning("[DbBoot] zoneName is invalid when trying to create starter warehouse.");
            return;
        }

        var player = Db.Table<Player>().FirstOrDefault();
        if (player == null)
        {
            Debug.LogWarning("[DbBoot] No player found when trying to create starter warehouse.");
            return;
        }

        var existingStarter = Db.Table<Warehouse>()
            .FirstOrDefault(w => w.playerId == player.id && w.isStarterWarehouse == 1);

        if (existingStarter != null)
            return;

        var sameTile = Db.Table<Warehouse>()
            .FirstOrDefault(w =>
                w.playerId == player.id &&
                w.zoneName == zoneName &&
                w.tileX == tileX &&
                w.tileZ == tileZ);

        if (sameTile != null)
            return;

        Db.Insert(new Warehouse
        {
            playerId = player.id,
            zoneName = zoneName,
            tileX = tileX,
            tileZ = tileZ,
            worldX = worldX,
            worldY = worldY,
            worldZ = worldZ,
            isStarterWarehouse = 1,
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });

        Debug.Log($"[DbBoot] Registered starter warehouse in '{zoneName}' at tile ({tileX}, {tileZ})");
    }

        private void EnsureVehicleSchema()
    {
        if (Db == null)
        {
            Debug.LogError("[DbBoot] Database connection is null in EnsureVehicleSchema.");
            return;
        }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN hasSavedLocation INTEGER NOT NULL DEFAULT 0");
            Debug.Log("[DbBoot] Added hasSavedLocation column to Vehicle");
        }
        catch (Exception) { }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN savedScene TEXT");
            Debug.Log("[DbBoot] Added savedScene column to Vehicle");
        }
        catch (Exception) { }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN savedX REAL NOT NULL DEFAULT 0");
            Debug.Log("[DbBoot] Added savedX column to Vehicle");
        }
        catch (Exception) { }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN savedY REAL NOT NULL DEFAULT 0");
            Debug.Log("[DbBoot] Added savedY column to Vehicle");
        }
        catch (Exception) { }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN savedZ REAL NOT NULL DEFAULT 0");
            Debug.Log("[DbBoot] Added savedZ column to Vehicle");
        }
        catch (Exception) { }

        try
        {
            Db.Execute("ALTER TABLE Vehicle ADD COLUMN savedYaw REAL NOT NULL DEFAULT 0");
            Debug.Log("[DbBoot] Added savedYaw column to Vehicle");
        }
        catch (Exception) { }
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