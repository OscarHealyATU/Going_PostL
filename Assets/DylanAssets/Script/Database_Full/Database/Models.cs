using SQLite;

[Table("Player")]
public class Player
{
    [PrimaryKey, AutoIncrement] public int id { get; set; }
    public string name { get; set; }
    public double money { get; set; }

    public int returnValid { get; set; }
    public double returnX { get; set; }
    public double returnY { get; set; }
    public double returnZ { get; set; }
    public double returnYaw { get; set; }

    public string createdAt { get; set; }
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

    // NEW — multi-scene spawn support
    public string spawnScene { get; set; }   // e.g. "MainWorld"
    public int spawnBay { get; set; }        // 0..3
    public int spawnPending { get; set; }    // 1 = pending, 0 = spawned
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
