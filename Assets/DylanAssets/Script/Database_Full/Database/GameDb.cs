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

        // ✅ Ensure newer columns exist even after deleting/recreating the DB
        EnsureVehicleSpawnColumns();

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

        // Keep CREATE TABLE minimal (like you had). We add new columns via migration below.
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
    }

    // ----------------------------
    // MIGRATIONS (safe updates)
    // ----------------------------
    private void EnsureVehicleSpawnColumns()
    {
        // Adds columns if missing. Safe to run every launch.
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
    }

    private class PragmaColumn
    {
        public string name { get; set; }
    }

    private void Seed()
    {
        // Seed VehicleTypes (idempotent)
        Db.Execute(@"
INSERT OR IGNORE INTO VehicleType (name, baseCost, baseHealth) VALUES
('Bicycle', 500.0, 80.0),
('3Wheeler', 2000.0, 120.0),
('eVan', 15000.0, 250.0),
('Lorry', 50000.0, 400.0);
");
    }

    public void Dispose()
    {
        try { Db?.Close(); } catch { }
        Db = null;
    }
}