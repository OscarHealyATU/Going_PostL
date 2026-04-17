using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WarehouseService
{
    public struct WarehousePurchaseResult
    {
        public bool success;
        public string message;
        public Warehouse warehouse;
    }

    public static Player GetPlayerOrNull()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return null;

        return DbBoot.Instance.Db.Table<Player>().FirstOrDefault();
    }

    public static List<Warehouse> GetAllOwned()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return new List<Warehouse>();

        var player = GetPlayerOrNull();
        if (player == null)
            return new List<Warehouse>();

        return DbBoot.Instance.Db.Table<Warehouse>()
            .Where(w => w.playerId == player.id)
            .OrderBy(w => w.id)
            .ToList();
    }

    public static List<Warehouse> GetOwnedInZone(string zoneName)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return new List<Warehouse>();

        var player = GetPlayerOrNull();
        if (player == null || string.IsNullOrWhiteSpace(zoneName))
            return new List<Warehouse>();

        return DbBoot.Instance.Db.Table<Warehouse>()
            .Where(w => w.playerId == player.id && w.zoneName == zoneName)
            .OrderBy(w => w.id)
            .ToList();
    }

    public static Warehouse GetStarterWarehouse()
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return null;

        var player = GetPlayerOrNull();
        if (player == null)
            return null;

        return DbBoot.Instance.Db.Table<Warehouse>()
            .FirstOrDefault(w => w.playerId == player.id && w.isStarterWarehouse == 1);
    }

    public static bool HasWarehouseAtTile(string zoneName, int tileX, int tileZ)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        var player = GetPlayerOrNull();
        if (player == null || string.IsNullOrWhiteSpace(zoneName))
            return false;

        return DbBoot.Instance.Db.Table<Warehouse>()
            .Any(w =>
                w.playerId == player.id &&
                w.zoneName == zoneName &&
                w.tileX == tileX &&
                w.tileZ == tileZ);
    }

    public static Warehouse GetWarehouseAtTile(string zoneName, int tileX, int tileZ)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return null;

        var player = GetPlayerOrNull();
        if (player == null || string.IsNullOrWhiteSpace(zoneName))
            return null;

        return DbBoot.Instance.Db.Table<Warehouse>()
            .FirstOrDefault(w =>
                w.playerId == player.id &&
                w.zoneName == zoneName &&
                w.tileX == tileX &&
                w.tileZ == tileZ);
    }

    public static Warehouse GetWarehouseById(int warehouseId)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null || warehouseId <= 0)
            return null;

        var player = GetPlayerOrNull();
        if (player == null)
            return null;

        return DbBoot.Instance.Db.Table<Warehouse>()
            .FirstOrDefault(w => w.id == warehouseId && w.playerId == player.id);
    }

    public static Warehouse GetLastInteractedWarehouse()
    {
        var player = GetPlayerOrNull();
        if (player == null || player.lastWarehouseId <= 0)
            return null;

        return GetWarehouseById(player.lastWarehouseId);
    }

    public static bool SetLastInteractedWarehouse(int warehouseId)
    {
        if (warehouseId <= 0)
            return false;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        var db = DbBoot.Instance.Db;
        var player = GetPlayerOrNull();
        if (player == null)
            return false;

        var warehouse = db.Table<Warehouse>()
            .FirstOrDefault(w => w.id == warehouseId && w.playerId == player.id);

        if (warehouse == null)
            return false;

        player.lastWarehouseId = warehouse.id;
        db.Update(player);

        Debug.Log($"[WarehouseService] Set last interacted warehouse to ID {warehouse.id}");
        return true;
    }

    public static bool SetLastInteractedWarehouse(string zoneName, int tileX, int tileZ)
    {
        Warehouse warehouse = GetWarehouseAtTile(zoneName, tileX, tileZ);
        if (warehouse == null)
            return false;

        return SetLastInteractedWarehouse(warehouse.id);
    }

    public static void EnsureStarterWarehouse(
        string zoneName,
        int tileX,
        int tileZ,
        float worldX,
        float worldY,
        float worldZ)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            Debug.LogWarning("[WarehouseService] DB unavailable.");
            return;
        }

        if (string.IsNullOrWhiteSpace(zoneName))
        {
            Debug.LogWarning("[WarehouseService] zoneName is invalid.");
            return;
        }

        var db = DbBoot.Instance.Db;
        var player = GetPlayerOrNull();
        if (player == null)
        {
            Debug.LogWarning("[WarehouseService] No player row found.");
            return;
        }

        var existingStarter = db.Table<Warehouse>()
            .FirstOrDefault(w => w.playerId == player.id && w.isStarterWarehouse == 1);

        if (existingStarter != null)
        {
            if (player.lastWarehouseId <= 0)
            {
                player.lastWarehouseId = existingStarter.id;
                db.Update(player);
            }

            return;
        }

        var sameTile = db.Table<Warehouse>()
            .FirstOrDefault(w =>
                w.playerId == player.id &&
                w.zoneName == zoneName &&
                w.tileX == tileX &&
                w.tileZ == tileZ);

        if (sameTile != null)
        {
            if (player.lastWarehouseId <= 0)
            {
                player.lastWarehouseId = sameTile.id;
                db.Update(player);
            }

            return;
        }

        var warehouse = new Warehouse
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
        };

        db.Insert(warehouse);

        if (player.lastWarehouseId <= 0)
        {
            player.lastWarehouseId = warehouse.id;
            db.Update(player);
        }

        Debug.Log($"[WarehouseService] Starter warehouse saved at zone '{zoneName}' tile ({tileX}, {tileZ})");
    }

    public static WarehousePurchaseResult TryPurchaseWarehouse(
        string zoneName,
        int tileX,
        int tileZ,
        float worldX,
        float worldY,
        float worldZ,
        double price = 0.0)
    {
        WarehousePurchaseResult result = new WarehousePurchaseResult
        {
            success = false,
            message = "Purchase failed.",
            warehouse = null
        };

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            result.message = "Database not available.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(zoneName))
        {
            result.message = "Zone is invalid.";
            return result;
        }

        var db = DbBoot.Instance.Db;
        var player = GetPlayerOrNull();
        if (player == null)
        {
            result.message = "Player not found.";
            return result;
        }

        if (tileX < 0 || tileZ < 0)
        {
            result.message = "Tile coordinates are invalid.";
            return result;
        }

        if (HasWarehouseAtTile(zoneName, tileX, tileZ))
        {
            result.message = "A warehouse already exists on that tile.";
            return result;
        }

        if (player.money < price)
        {
            result.message = "Not enough money.";
            return result;
        }

        var warehouse = new Warehouse
        {
            playerId = player.id,
            zoneName = zoneName,
            tileX = tileX,
            tileZ = tileZ,
            worldX = worldX,
            worldY = worldY,
            worldZ = worldZ,
            isStarterWarehouse = 0,
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        try
        {
            db.RunInTransaction(() =>
            {
                if (price > 0.0)
                {
                    player.money -= price;
                    db.Update(player);

                    db.Insert(new TransactionLog
                    {
                        playerId = player.id,
                        type = "warehouse_purchase",
                        amount = -price,
                        description = $"Purchased warehouse at {zoneName} {tileX} {tileZ}",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                db.Insert(warehouse);
            });

            if (price > 0.0 && DayManager.Instance != null)
                DayManager.Instance.RegisterMoneySpent(price);

            result.success = true;
            result.message = "Warehouse purchased successfully.";
            result.warehouse = warehouse;
            return result;
        }
        catch (Exception ex)
        {
            result.message = $"Warehouse purchase failed: {ex.Message}";
            return result;
        }
    }

    public static Vector3 TileToWorld(gridify grid, int tileX, int tileZ)
    {
        if (grid == null)
            return Vector3.zero;

        return new Vector3(
            grid.xStartPosition + tileX * grid.distance,
            0f,
            grid.zStartPosition + tileZ * grid.distance
        );
    }

    public static bool TryWorldToTile(gridify grid, Vector3 worldPosition, out int tileX, out int tileZ)
    {
        tileX = -1;
        tileZ = -1;

        if (grid == null || grid.distance <= 0f)
            return false;

        float rawX = (worldPosition.x - grid.xStartPosition) / grid.distance;
        float rawZ = (worldPosition.z - grid.zStartPosition) / grid.distance;

        int roundedX = Mathf.RoundToInt(rawX);
        int roundedZ = Mathf.RoundToInt(rawZ);

        if (roundedX < 0 || roundedZ < 0)
            return false;

        if (roundedX >= Mathf.RoundToInt(grid.noOfHousesX) || roundedZ >= Mathf.RoundToInt(grid.noOfHousesZ))
            return false;

        tileX = roundedX;
        tileZ = roundedZ;
        return true;
    }

    public static string FormatTileLabel(string zoneName, int tileX, int tileZ)
    {
        return $"{zoneName} {tileX}, {tileZ}";
    }
}