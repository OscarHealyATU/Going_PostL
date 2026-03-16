using SQLite;

[Table("Player")]
public class Player
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double money { get; set; }
    public string createdAt { get; set; }

    // existing project fields
    public int returnValid { get; set; }
    public float returnX { get; set; }
    public float returnY { get; set; }
    public float returnZ { get; set; }
    public float returnYaw { get; set; }
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
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int playerId { get; set; }
    public int slotIndex { get; set; }
    public string itemKey { get; set; }
    public string itemName { get; set; }
}

[Table("Deliverable")]
public class Deliverable
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public int playerId { get; set; }
    public int itemTypeId { get; set; }       
    public int sourceItemTypeId { get; set; } 
    public string assignedScene { get; set; }
    public string assignedGridKey { get; set; }
    public string status { get; set; }        
    public string createdAt { get; set; }
    public string deliveredAt { get; set; }
}