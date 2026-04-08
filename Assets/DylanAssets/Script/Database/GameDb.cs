using System;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

public sealed class GameDb : IDisposable
{
    public SQLiteConnection Db { get; private set; }

    public static string DbPath =>
        Path.Combine(Application.persistentDataPath, "delivery_game.db");

    public GameDb()
    {
        Db = new SQLiteConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        Db.Execute("PRAGMA foreign_keys = ON;");

        ApplySchema();
        EnsurePlayerColumns();
        EnsurePlayerResumeColumns();
        EnsureVehicleTypeColumns();
        EnsureVehicleSpawnColumns();
        EnsureDayStateColumns();
        EnsureDeliveryJobColumns();
        EnsureDeliveryZoneColumns();

        Seed();
        EnsureDayStateRow();
    }

    private void ApplySchema()
    {
        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS Player (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          name TEXT NOT NULL,
          money REAL NOT NULL DEFAULT 0.0,
          totalExperience INTEGER NOT NULL DEFAULT 0,
          createdAt TEXT NOT NULL DEFAULT (datetime('now'))
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS VehicleType (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          name TEXT NOT NULL UNIQUE,
          baseCost REAL NOT NULL,
          storageCapacity INTEGER NOT NULL DEFAULT 0,
          baseHealth REAL NOT NULL DEFAULT 100.0
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS Vehicle (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          vehicleTypeId INTEGER NOT NULL,
          ownedByPlayerId INTEGER NOT NULL,
          maxHealth REAL NOT NULL,
          currentHealth REAL NOT NULL,
          purchasedAt TEXT NOT NULL DEFAULT (datetime('now')),
          spawnScene TEXT,
          spawnBay INTEGER,
          spawnPending INTEGER NOT NULL DEFAULT 1,
          FOREIGN KEY(vehicleTypeId) REFERENCES VehicleType(id),
          FOREIGN KEY(ownedByPlayerId) REFERENCES Player(id)
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS TransactionLog (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          playerId INTEGER NOT NULL,
          type TEXT NOT NULL,
          amount REAL NOT NULL,
          description TEXT,
          timestamp TEXT NOT NULL DEFAULT (datetime('now')),
          FOREIGN KEY(playerId) REFERENCES Player(id)
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS ItemType (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          key TEXT NOT NULL UNIQUE,
          name TEXT NOT NULL,
          category TEXT NOT NULL,
          stackable INTEGER NOT NULL DEFAULT 1,
          baseValue REAL NOT NULL DEFAULT 0.0
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS InventorySlot (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          playerId INTEGER NOT NULL,
          slotIndex INTEGER NOT NULL,
          itemKey TEXT,
          itemName TEXT,
          FOREIGN KEY(playerId) REFERENCES Player(id)
        );");

        Db.Execute(@"
        CREATE UNIQUE INDEX IF NOT EXISTS idx_inventoryslot_player_slot
        ON InventorySlot(playerId, slotIndex);");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS DeliveryJob (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          itemId TEXT NOT NULL,
          itemName TEXT,
          status INTEGER NOT NULL DEFAULT 0,
          targetX REAL NOT NULL,
          targetY REAL NOT NULL,
          targetZ REAL NOT NULL,
          zoneId INTEGER NOT NULL DEFAULT 1,
          createdAt TEXT NOT NULL DEFAULT (datetime('now'))
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS DayState (
          id INTEGER PRIMARY KEY,
          dayNumber INTEGER NOT NULL DEFAULT 1,
          currentMinuteOfDay INTEGER NOT NULL DEFAULT 540,
          isDayEnded INTEGER NOT NULL DEFAULT 0,
          packagesDeliveredToday INTEGER NOT NULL DEFAULT 0,
          moneyEarnedToday REAL NOT NULL DEFAULT 0.0,
          moneySpentToday REAL NOT NULL DEFAULT 0.0,
          totalRevenueToday REAL NOT NULL DEFAULT 0.0,
          experienceEarnedToday INTEGER NOT NULL DEFAULT 0
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS DeliveryZone (
          id INTEGER PRIMARY KEY,
          name TEXT NOT NULL,
          unlockCost INTEGER NOT NULL DEFAULT 0,
          requiredLevel INTEGER NOT NULL DEFAULT 1,
          payMultiplier REAL NOT NULL DEFAULT 1.0,
          xpMultiplier REAL NOT NULL DEFAULT 1.0,
          startsUnlocked INTEGER NOT NULL DEFAULT 0
        );");

        Db.Execute(@"
        CREATE TABLE IF NOT EXISTS PlayerZoneUnlock (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          playerId INTEGER NOT NULL,
          zoneId INTEGER NOT NULL,
          unlockedAt TEXT NOT NULL DEFAULT (datetime('now')),
          FOREIGN KEY(playerId) REFERENCES Player(id),
          FOREIGN KEY(zoneId) REFERENCES DeliveryZone(id)
        );");

        Db.Execute(@"
        CREATE UNIQUE INDEX IF NOT EXISTS idx_playerzoneunlock_player_zone
        ON PlayerZoneUnlock(playerId, zoneId);");
    }

    private void EnsurePlayerColumns()
    {
        AddColumnIfMissing("Player", "totalExperience", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "returnValid", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "returnX", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "returnY", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "returnZ", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "returnYaw", "REAL NOT NULL DEFAULT 0");
    }

    private void EnsurePlayerResumeColumns()
    {
        AddColumnIfMissing("Player", "hasResumePoint", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "savedScene", "TEXT");
        AddColumnIfMissing("Player", "savedX", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "savedY", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "savedZ", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing("Player", "savedYaw", "REAL NOT NULL DEFAULT 0");
    }

    private void EnsureVehicleTypeColumns()
    {
        AddColumnIfMissing("VehicleType", "storageCapacity", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("VehicleType", "baseHealth", "REAL NOT NULL DEFAULT 100.0");
    }

    private void EnsureVehicleSpawnColumns()
    {
        AddColumnIfMissing("Vehicle", "spawnScene", "TEXT");
        AddColumnIfMissing("Vehicle", "spawnBay", "INTEGER");
        AddColumnIfMissing("Vehicle", "spawnPending", "INTEGER NOT NULL DEFAULT 1");
    }

    private void EnsureDayStateColumns()
    {
        AddColumnIfMissing("DayState", "dayNumber", "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing("DayState", "currentMinuteOfDay", "INTEGER NOT NULL DEFAULT 540");
        AddColumnIfMissing("DayState", "isDayEnded", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("DayState", "packagesDeliveredToday", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("DayState", "moneyEarnedToday", "REAL NOT NULL DEFAULT 0.0");
        AddColumnIfMissing("DayState", "moneySpentToday", "REAL NOT NULL DEFAULT 0.0");
        AddColumnIfMissing("DayState", "totalRevenueToday", "REAL NOT NULL DEFAULT 0.0");
        AddColumnIfMissing("DayState", "experienceEarnedToday", "INTEGER NOT NULL DEFAULT 0");
    }

    private void EnsureDeliveryJobColumns()
    {
        AddColumnIfMissing("DeliveryJob", "zoneId", "INTEGER NOT NULL DEFAULT 1");
    }

    private void EnsureDeliveryZoneColumns()
    {
        AddColumnIfMissing("DeliveryZone", "unlockCost", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("DeliveryZone", "requiredLevel", "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing("DeliveryZone", "payMultiplier", "REAL NOT NULL DEFAULT 1.0");
        AddColumnIfMissing("DeliveryZone", "xpMultiplier", "REAL NOT NULL DEFAULT 1.0");
        AddColumnIfMissing("DeliveryZone", "startsUnlocked", "INTEGER NOT NULL DEFAULT 0");
    }

    private void AddColumnIfMissing(string table, string column, string columnSql)
    {
        var cols = Db.Query<PragmaColumn>($"PRAGMA table_info({table});");
        bool exists = cols.Any(c => c.name == column);
        if (exists) return;

        Db.Execute($"ALTER TABLE {table} ADD COLUMN {column} {columnSql};");
        Debug.Log($"[GameDb] Added missing column {table}.{column}");
    }

    private class PragmaColumn
    {
        public string name { get; set; }
    }

    private void Seed()
    {
        SeedVehicleTypes();
        SeedItemTypes();
        SeedDeliveryZones();
    }

    private void SeedVehicleTypes()
    {
        UpsertVehicleType("Bicycle", 500.0, 5, 80.0);
        UpsertVehicleType("3Wheeler", 2000.0, 20, 120.0);
        UpsertVehicleType("eVan", 15000.0, 60, 250.0);
        UpsertVehicleType("Lorry", 50000.0, 150, 400.0);

        DeleteVehicleTypeIfExists("Zone 1");
        DeleteVehicleTypeIfExists("Zone 2");
        DeleteVehicleTypeIfExists("Zone 3");
        DeleteVehicleTypeIfExists("Zone 4");
        DeleteVehicleTypeIfExists("Zone 5");
        DeleteVehicleTypeIfExists("Zone 6");
    }

    private void UpsertVehicleType(string name, double baseCost, int storageCapacity, double baseHealth)
    {
        var existing = Db.Table<VehicleType>().FirstOrDefault(v => v.name == name);

        if (existing == null)
        {
            Db.Insert(new VehicleType
            {
                name = name,
                baseCost = baseCost,
                storageCapacity = storageCapacity,
                baseHealth = baseHealth
            });

            Debug.Log($"[GameDb] Seeded VehicleType '{name}'");
            return;
        }

        bool changed = false;

        if (Math.Abs(existing.baseCost - baseCost) > 0.001)
        {
            existing.baseCost = baseCost;
            changed = true;
        }

        if (existing.storageCapacity != storageCapacity)
        {
            existing.storageCapacity = storageCapacity;
            changed = true;
        }

        if (Math.Abs(existing.baseHealth - baseHealth) > 0.001)
        {
            existing.baseHealth = baseHealth;
            changed = true;
        }

        if (changed)
        {
            Db.Update(existing);
            Debug.Log($"[GameDb] Updated VehicleType '{name}'");
        }
    }

    private void DeleteVehicleTypeIfExists(string name)
    {
        var existing = Db.Table<VehicleType>().FirstOrDefault(v => v.name == name);
        if (existing == null) return;

        Db.Delete(existing);
        Debug.Log($"[GameDb] Removed old VehicleType '{name}'");
    }

    private void SeedItemTypes()
    {
        Db.Execute(@"
INSERT OR IGNORE INTO ItemType (key, name, category, stackable, baseValue) VALUES
('box_open', 'Box Open', 'packingItem', 0, 5.0),
('box_close', 'Box Close', 'packingItem', 0, 25.0),
('ball', 'Ball', 'mediumItem', 0, 10.0),
('book_beige', 'Book Beige', 'mediumItem', 0, 12.0),
('book_blue', 'Book Blue', 'mediumItem', 0, 12.0),
('book_red', 'Book Red', 'mediumItem', 0, 12.0),
('console', 'Console', 'mediumItem', 0, 40.0),
('headphones_black', 'Headphones Black', 'mediumItem', 0, 25.0),
('headphones_white', 'Headphones White', 'mediumItem', 0, 25.0),
('headphones_green', 'Headphones Green', 'mediumItem', 0, 25.0),
('headphones_pink', 'Headphones Pink', 'mediumItem', 0, 25.0),
('lamp', 'Lamp', 'mediumItem', 0, 18.0),
('laptop_grey', 'Laptop Grey', 'mediumItem', 0, 50.0),
('laptop_navy', 'Laptop Navy', 'mediumItem', 0, 50.0),
('photo_frame_metal', 'Photo Frame Metal', 'mediumItem', 0, 15.0),
('photo_frame_wood', 'Photo Frame Wood', 'mediumItem', 0, 15.0),
('toaster', 'Toaster', 'mediumItem', 0, 20.0);");
    }

    private void SeedDeliveryZones()
    {
        UpsertDeliveryZone(1, "Zone 1", 0,     1, 1.00f, 1.00f, 1);
        UpsertDeliveryZone(2, "Zone 2", 2500,  2, 1.20f, 1.15f, 0);
        UpsertDeliveryZone(3, "Zone 3", 6000,  4, 1.45f, 1.30f, 0);
        UpsertDeliveryZone(4, "Zone 4", 11000, 6, 1.75f, 1.50f, 0);
        UpsertDeliveryZone(5, "Zone 5", 18000, 8, 2.10f, 1.75f, 0);
        UpsertDeliveryZone(6, "Zone 6", 30000, 10, 2.50f, 2.10f, 0);
    }

    private void UpsertDeliveryZone(
        int id,
        string name,
        int unlockCost,
        int requiredLevel,
        float payMultiplier,
        float xpMultiplier,
        int startsUnlocked)
    {
        var existing = Db.Find<DeliveryZone>(id);

        if (existing == null)
        {
            Db.Insert(new DeliveryZone
            {
                id = id,
                name = name,
                unlockCost = unlockCost,
                requiredLevel = requiredLevel,
                payMultiplier = payMultiplier,
                xpMultiplier = xpMultiplier,
                startsUnlocked = startsUnlocked
            });

            Debug.Log($"[GameDb] Seeded DeliveryZone '{name}'");
            return;
        }

        bool changed = false;

        if (existing.name != name) { existing.name = name; changed = true; }
        if (existing.unlockCost != unlockCost) { existing.unlockCost = unlockCost; changed = true; }
        if (existing.requiredLevel != requiredLevel) { existing.requiredLevel = requiredLevel; changed = true; }
        if (Math.Abs(existing.payMultiplier - payMultiplier) > 0.001f) { existing.payMultiplier = payMultiplier; changed = true; }
        if (Math.Abs(existing.xpMultiplier - xpMultiplier) > 0.001f) { existing.xpMultiplier = xpMultiplier; changed = true; }
        if (existing.startsUnlocked != startsUnlocked) { existing.startsUnlocked = startsUnlocked; changed = true; }

        if (changed)
        {
            Db.Update(existing);
            Debug.Log($"[GameDb] Updated DeliveryZone '{name}'");
        }
    }

    private void EnsureDayStateRow()
    {
        var existing = Db.Table<DayState>().FirstOrDefault();
        if (existing != null) return;

        Db.Insert(new DayState
        {
            id = 1,
            dayNumber = 1,
            currentMinuteOfDay = 9 * 60,
            isDayEnded = 0,
            packagesDeliveredToday = 0,
            moneyEarnedToday = 0.0,
            moneySpentToday = 0.0,
            totalRevenueToday = 0.0,
            experienceEarnedToday = 0
        });

        Debug.Log("[GameDb] Created default DayState row");
    }

    public void Dispose()
    {
        try { Db?.Close(); } catch { }
        Db = null;
    }
}