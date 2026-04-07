using System;
using System.Linq;

public static class VehicleService
{
    public struct PurchaseResult
    {
        public bool success;
        public string message;
        public Vehicle purchasedVehicle;
        public int usedSpawnPointIndex;
    }

    public static PurchaseResult TryPurchaseVehicleAndSpawn(int vehicleTypeId)
    {
        return TryPurchaseVehicleForScene(vehicleTypeId, "Main");
    }

    public static PurchaseResult TryPurchaseVehicleForScene(int vehicleTypeId, string targetSpawnScene)
    {
        PurchaseResult result = new PurchaseResult
        {
            success = false,
            message = "Purchase failed.",
            purchasedVehicle = null,
            usedSpawnPointIndex = -1
        };

        if (DbBoot.Instance == null)
        {
            result.message = "Database not available.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(targetSpawnScene))
        {
            result.message = "Target spawn scene is invalid.";
            return result;
        }

        var db = DbBoot.Instance.Db;
        var player = PlayerService.Get();
        var vehicleType = db.Find<VehicleType>(vehicleTypeId);

        if (vehicleType == null)
        {
            result.message = "Vehicle type not found.";
            return result;
        }

        int reservedBay = GetFirstAvailableBayForScene(targetSpawnScene);
        if (reservedBay < 0)
        {
            result.message = "All vehicle spawn points are currently occupied.";
            return result;
        }

        if (player.money < vehicleType.baseCost)
        {
            result.message = $"Not enough money. Need €{vehicleType.baseCost:0}.";
            return result;
        }

        Vehicle createdVehicle = null;
        bool purchaseCommitted = false;

        db.RunInTransaction(() =>
        {
            player.money -= vehicleType.baseCost;
            db.Update(player);

            createdVehicle = new Vehicle
            {
                vehicleTypeId = vehicleType.id,
                ownedByPlayerId = player.id,
                maxHealth = vehicleType.baseHealth,
                currentHealth = vehicleType.baseHealth,
                purchasedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                spawnScene = targetSpawnScene,
                spawnBay = reservedBay,
                spawnPending = 1
            };

            db.Insert(createdVehicle);

            db.Insert(new TransactionLog
            {
                playerId = player.id,
                type = "vehicle_buy",
                amount = -vehicleType.baseCost,
                description = $"Bought {vehicleType.name} reserved for bay {reservedBay + 1} in scene {targetSpawnScene}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            purchaseCommitted = true;
        });

        if (!purchaseCommitted || createdVehicle == null)
        {
            result.message = "Purchase failed.";
            return result;
        }

        if (DayManager.Instance != null)
            DayManager.Instance.RegisterMoneySpent(vehicleType.baseCost);

        PlayerService.RefreshAllUI();

        result.success = true;
        result.message = $"{vehicleType.name} purchased. Reserved for spawn point {reservedBay + 1}.";
        result.purchasedVehicle = createdVehicle;
        result.usedSpawnPointIndex = reservedBay;
        return result;
    }

    public static int GetFirstAvailableBayForScene(string targetSpawnScene)
    {
        if (DbBoot.Instance == null || string.IsNullOrWhiteSpace(targetSpawnScene))
            return -1;

        var db = DbBoot.Instance.Db;

        var reservedBays = db.Table<Vehicle>()
            .Where(v => v.spawnScene == targetSpawnScene && v.spawnBay >= 0)
            .Select(v => v.spawnBay)
            .ToList();

        for (int i = 0; i < 4; i++)
        {
            if (!reservedBays.Contains(i))
                return i;
        }

        return -1;
    }

    public static VehicleType[] GetAllVehicleTypes()
    {
        if (DbBoot.Instance == null)
            return Array.Empty<VehicleType>();

        return DbBoot.Instance.Db.Table<VehicleType>()
            .OrderBy(v => v.baseCost)
            .ToArray();
    }
}