using SQLite;

[Table("Player")]
public class Player
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double money { get; set; }
    public int totalExperience {get; set; }
    public string createdAt { get; set; }

    
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
}

[Table("VehicleType")]
public class VehicleType
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double baseCost { get; set; }
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

    public string createdAt { get; set; }
}