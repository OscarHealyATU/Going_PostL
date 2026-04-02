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
        EnsureVehicleSpawnColumns();
        EnsurePlayerResumeColumns();
        Seed();
    }

    private void ApplySchema()
    {
        Db.Execute(@"
CREATE TABLE IF NOT EXISTS Player (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  money REAL NOT NULL DEFAULT 0.0,
  createdAt TEXT NOT NULL DEFAULT (datetime('now'))
);");

        Db.Execute(@"
CREATE TABLE IF NOT EXISTS VehicleType (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  baseCost REAL NOT NULL,
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
  createdAt TEXT NOT NULL DEFAULT (datetime('now'))
);");
    }

    private void EnsurePlayerColumns()
    {
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

    private void EnsureVehicleSpawnColumns()
    {
        AddColumnIfMissing("Vehicle", "spawnScene", "TEXT");
        AddColumnIfMissing("Vehicle", "spawnBay", "INTEGER");
        AddColumnIfMissing("Vehicle", "spawnPending", "INTEGER NOT NULL DEFAULT 1");
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
        Db.Execute(@"
INSERT OR IGNORE INTO VehicleType (name, baseCost, baseHealth) VALUES
('Bicycle', 500.0, 80.0),
('3Wheeler', 2000.0, 120.0),
('eVan', 15000.0, 250.0),
('Lorry', 50000.0, 400.0);");

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

    public void Dispose()
    {
        try { Db?.Close(); } catch { }
        Db = null;
    }
}