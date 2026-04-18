using SQLite;
using UnityEngine;

[Table("Player")]
public class Player
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double money { get; set; }
    public int totalExperience { get; set; }
    public string createdAt { get; set; }

    public int inventorySlotCount { get; set; }

    public int returnValid { get; set; }
    public float returnX { get; set; }
    public float returnY { get; set; }
    public float returnZ { get; set; }
    public float returnYaw { get; set; }

    public int hasResumePoint { get; set; }
    public string savedScene { get; set; }
    public float savedX { get; set; }
    public float savedY { get; set; }
    public float savedZ { get; set; }
    public float savedYaw { get; set; }

    // NEW: the warehouse the player most recently interacted with
    public int lastWarehouseId { get; set; }
}

[Table("VehicleType")]
public class VehicleType
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double baseCost { get; set; }
    public int storageCapacity { get; set; }
    public double baseHealth { get; set; }
}

[Table("Vehicle")]
public class Vehicle
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }

    public int vehicleTypeId { get; set; }
    public int ownedByPlayerId { get; set; }

    public double maxHealth { get; set; }
    public double currentHealth { get; set; }

    public string purchasedAt { get; set; }

    public string spawnScene { get; set; }
    public int spawnBay { get; set; }
    public int spawnPending { get; set; }

    // Saved parked location
    public int hasSavedLocation { get; set; }
    public string savedScene { get; set; }
    public float savedX { get; set; }
    public float savedY { get; set; }
    public float savedZ { get; set; }
    public float savedYaw { get; set; }
}

[Table("StoredDelivery")]
public class StoredDelivery
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }

    public int vehicleId { get; set; }
    public int originalDeliveryJobId { get; set; }

    public string itemId { get; set; }
    public string itemName { get; set; }

    public float targetX { get; set; }
    public float targetY { get; set; }
    public float targetZ { get; set; }

    public int zoneId { get; set; }

    public int slotIndex { get; set; }

    public string storedAt { get; set; }
}

[Table("TransactionLog")]
public class TransactionLog
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public int playerId { get; set; }
    public string type { get; set; }
    public double amount { get; set; }
    public string description { get; set; }
    public string timestamp { get; set; }
}

[Table("ItemType")]
public class ItemType
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    [Unique] public string key { get; set; }
    public string name { get; set; }
    public string category { get; set; }
    public int stackable { get; set; }
    public double baseValue { get; set; }
}

[Table("InventorySlot")]
public class InventorySlot
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public int playerId { get; set; }
    public int slotIndex { get; set; }
    public string itemKey { get; set; }
    public string itemName { get; set; }
}

[Table("DeliveryJob")]
public class DeliveryJob
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }

    public string itemId { get; set; }
    public string itemName { get; set; }
    public int status { get; set; }

    public float targetX { get; set; }
    public float targetY { get; set; }
    public float targetZ { get; set; }

    public int zoneId { get; set; }

    public string createdAt { get; set; }
}

[Table("DayState")]
public class DayState
{
    [PrimaryKey] public int id { get; set; } = 1;

    public int dayNumber { get; set; } = 1;
    public int currentMinuteOfDay { get; set; } = 9 * 60;
    public int isDayEnded { get; set; } = 0;

    public int packagesDeliveredToday { get; set; } = 0;
    public double moneyEarnedToday { get; set; } = 0;
    public double moneySpentToday { get; set; } = 0;
    public double totalRevenueToday { get; set; } = 0;
    public int experienceEarnedToday { get; set; } = 0;
}

[Table("DeliveryZone")]
public class DeliveryZone
{
    [PrimaryKey] public int id { get; set; }
    public string name { get; set; }
    public int unlockCost { get; set; }
    public int requiredLevel { get; set; }
    public float payMultiplier { get; set; }
    public float xpMultiplier { get; set; }
    public int startsUnlocked { get; set; }
}

[Table("PlayerZoneUnlock")]
public class PlayerZoneUnlock
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public int playerId { get; set; }
    public int zoneId { get; set; }
    public string unlockedAt { get; set; }
}

[Table("Warehouse")]
public class Warehouse
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }

    public int playerId { get; set; }

    public string zoneName { get; set; }

    public int tileX { get; set; }
    public int tileZ { get; set; }

    public float worldX { get; set; }
    public float worldY { get; set; }
    public float worldZ { get; set; }

    public int isStarterWarehouse { get; set; }

    public string createdAt { get; set; }
}

public enum UpgradeType
{
    ZoneLicense,
    Storage
}

[System.Serializable]
public class UpgradeDefinition
{
    public UpgradeType upgradeType = UpgradeType.ZoneLicense;

    [Header("Display")]
    public string title;
    [TextArea(2, 4)] public string description;

    [Header("Requirements")]
    public int requiredLevel = 1;
    public int price = 0;

    [Header("Zone License")]
    public int zoneId = 1;

    [Header("Storage")]
    public int inventorySlotIncrease = 0;
}