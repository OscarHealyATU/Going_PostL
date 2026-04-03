using System;
using System.Linq;

public static class VehicleService
{
    /// <summary>
    /// Returns true if a bay already has a vehicle assigned to it for that scene.
    /// This counts BOTH pending and already-spawned vehicles (so the bay stays occupied permanently).
    /// </summary>
    public static bool IsBayOccupied(string sceneName, int bay0Based)
    {
        if (DbBoot.Instance == null) return false;

        var db = DbBoot.Instance.Db;

        return db.Table<Vehicle>()
            .Any(v =>
                v.spawnScene == sceneName &&
                v.spawnBay == bay0Based &&
                // if you want per-player bays later, include: v.ownedByPlayerId == PlayerService.Get().id
                true
            );
    }

    public static Vehicle BuyVehicleQueuedForWorld(
        int vehicleTypeId,
        string spawnScene,
        int spawnBay0Based)
    {
        if (string.IsNullOrWhiteSpace(spawnScene))
            throw new Exception("spawnScene is missing.");

        if (spawnBay0Based < 0 || spawnBay0Based > 3)
            throw new Exception("Invalid bay selected (must be 1–4).");

        var db = DbBoot.Instance.Db;
        var player = PlayerService.Get();

        // ✅ Block purchase if bay already occupied
        if (IsBayOccupied(spawnScene, spawnBay0Based))
            throw new Exception($"Bay {spawnBay0Based + 1} is already occupied.");

        var type = db.Find<VehicleType>(vehicleTypeId);
        if (type == null)
            throw new Exception("VehicleType not found");

        if (player.money < type.baseCost)
            throw new Exception($"Not enough money. Need €{type.baseCost:0}, have €{player.money:0}");

        Vehicle created = null;

        db.RunInTransaction(() =>
        {
            // Re-check in transaction to avoid edge cases (double-click / two UI calls)
            bool stillFree = !db.Table<Vehicle>()
                .Any(v => v.spawnScene == spawnScene && v.spawnBay == spawnBay0Based);

            if (!stillFree)
                throw new Exception($"Bay {spawnBay0Based + 1} is already occupied.");

            // 1) Deduct money
            player.money -= type.baseCost;
            db.Update(player);

            // 2) Create vehicle instance (queued for spawn)
            created = new Vehicle
            {
                vehicleTypeId = type.id,
                ownedByPlayerId = player.id,

                maxHealth = type.baseHealth,
                currentHealth = type.baseHealth,
                purchasedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                spawnScene = spawnScene,
                spawnBay = spawnBay0Based,
                spawnPending = 1
            };

            db.Insert(created);

            // 3) Log transaction
            db.Insert(new TransactionLog
            {
                playerId = player.id,
                type = "vehicle_buy",
                amount = -type.baseCost,
                description = $"Bought {type.name} (queued for {spawnScene}, bay {spawnBay0Based + 1})",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
        });

        return created;
    }
}