using System;
using System.IO;
using UnityEngine;
using SQLite;

namespace DeliveryGame.Database
{
    /// <summary>
    /// Manages SQLite database connection and initialization.
    /// Uses the unity-sqlite-net package (com.gilzoide.sqlite-net).
    /// Attach to a persistent GameObject in your scene.
    /// </summary>
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        [SerializeField] private string databaseName = "DeliveryGame.db";
        
        private string _dbPath;

        public string DatabasePath => _dbPath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            _dbPath = Path.Combine(Application.persistentDataPath, databaseName);

            bool isNewDatabase = !File.Exists(_dbPath);

            if (isNewDatabase)
            {
                Debug.Log($"Creating new database at: {_dbPath}");
                CreateDatabase();
            }
            else
            {
                Debug.Log($"Database found at: {_dbPath}");
            }
        }

        private void CreateDatabase()
        {
            using var db = GetConnection();
            
            // Create all tables
            db.CreateTable<PlayerData>();
            db.CreateTable<ZoneData>();
            db.CreateTable<TileData>();
            db.CreateTable<VehicleTypeData>();
            db.CreateTable<VehicleData>();
            db.CreateTable<EmployeeRoleData>();
            db.CreateTable<EmployeeData>();
            db.CreateTable<ItemTypeData>();
            db.CreateTable<ItemData>();
            db.CreateTable<PackageData>();
            db.CreateTable<DeliveryData>();
            db.CreateTable<TransactionData>();

            // Seed initial data
            SeedData(db);

            Debug.Log("Database schema created successfully.");
        }

        private void SeedData(SQLiteConnection db)
        {
            // Insert zones
            db.InsertAll(new[]
            {
                new ZoneData { Name = "Downtown", BonusMultiplier = 1.5f, BaseCustomerDensity = 15 },
                new ZoneData { Name = "Suburbs", BonusMultiplier = 1.0f, BaseCustomerDensity = 10 },
                new ZoneData { Name = "Industrial", BonusMultiplier = 1.2f, BaseCustomerDensity = 8 },
                new ZoneData { Name = "Rural", BonusMultiplier = 0.8f, BaseCustomerDensity = 5 }
            });

            // Insert employee roles
            db.InsertAll(new[]
            {
                new EmployeeRoleData { Name = "packageOnLoader", Description = "Brings packages into the warehouse", BaseSalary = 80f },
                new EmployeeRoleData { Name = "packager", Description = "Boxes items up for delivery", BaseSalary = 100f },
                new EmployeeRoleData { Name = "packageOffLoader", Description = "Loads packages from warehouse to vehicles", BaseSalary = 90f }
            });

            // Insert vehicle types
            db.InsertAll(new[]
            {
                new VehicleTypeData { Name = "Bicycle", Capacity = 2, MaxSpeed = 15f, FuelEfficiency = 100f, BaseCost = 500f },
                new VehicleTypeData { Name = "Scooter", Capacity = 4, MaxSpeed = 35f, FuelEfficiency = 80f, BaseCost = 2000f },
                new VehicleTypeData { Name = "Van", Capacity = 20, MaxSpeed = 60f, FuelEfficiency = 40f, BaseCost = 15000f },
                new VehicleTypeData { Name = "Truck", Capacity = 50, MaxSpeed = 50f, FuelEfficiency = 25f, BaseCost = 50000f }
            });

            // Insert item types
            db.InsertAll(new[]
            {
                new ItemTypeData { Name = "Packing Table", Category = "equipment", BaseCost = 200f },
                new ItemTypeData { Name = "Conveyor Belt", Category = "equipment", BaseCost = 1500f },
                new ItemTypeData { Name = "Forklift", Category = "equipment", BaseCost = 8000f },
                new ItemTypeData { Name = "Shelving Unit", Category = "storage", BaseCost = 300f },
                new ItemTypeData { Name = "Pallet Rack", Category = "storage", BaseCost = 600f }
            });

            Debug.Log("Seed data inserted.");
        }

        /// <summary>
        /// Get a new database connection. Remember to dispose it when done!
        /// </summary>
        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_dbPath);
        }

        /// <summary>
        /// Reset the database (for testing or new game)
        /// </summary>
        public void ResetDatabase()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
                Debug.Log("Database deleted.");
            }

            CreateDatabase();
            Debug.Log("Database reset complete.");
        }
    }

    // ==========================================
    // DATA MODELS (using SQLite-net attributes)
    // ==========================================

    [Table("Player")]
    public class PlayerData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public float Money { get; set; } = 0f;
        public int Level { get; set; } = 1;
        public int Xp { get; set; } = 0;
        public int WarehouseScore { get; set; } = 0;
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
    }

    [Table("Zone")]
    public class ZoneData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public float BonusMultiplier { get; set; } = 1f;
        public int BaseCustomerDensity { get; set; } = 10;
    }

    [Table("Tile")]
    public class TileData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ZoneId { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        [Indexed]
        public int? OwnedByPlayerId { get; set; }
        public float PurchasePrice { get; set; } = 1000f;
        public int CustomerCount { get; set; } = 5;

        [Ignore]
        public bool IsOwned => OwnedByPlayerId.HasValue;
    }

    [Table("VehicleType")]
    public class VehicleTypeData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public float MaxSpeed { get; set; }
        public float FuelEfficiency { get; set; }
        public float BaseCost { get; set; }
    }

    [Table("Vehicle")]
    public class VehicleData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int VehicleTypeId { get; set; }
        [Indexed]
        public int OwnedByPlayerId { get; set; }
        public float CurrentFuel { get; set; } = 100f;
        public float DamageLevel { get; set; } = 0f;
        public float TotalMaintenanceCost { get; set; } = 0f;
        public string PurchasedAt { get; set; } = DateTime.Now.ToString("o");
    }

    [Table("EmployeeRole")]
    public class EmployeeRoleData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Unique]
        public string Name { get; set; }
        public string Description { get; set; }
        public float BaseSalary { get; set; } = 100f;
    }

    [Table("Employee")]
    public class EmployeeData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int PlayerId { get; set; }
        [Indexed]
        public int RoleId { get; set; }
        public string Name { get; set; }
        public float Salary { get; set; }
        public float Efficiency { get; set; } = 50f;
        public string HiredAt { get; set; } = DateTime.Now.ToString("o");
    }

    [Table("ItemType")]
    public class ItemTypeData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public float BaseCost { get; set; }
    }

    [Table("Item")]
    public class ItemData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ItemTypeId { get; set; }
        [Indexed]
        public int? OwnedByPlayerId { get; set; }
        public string PurchasedAt { get; set; }

        [Ignore]
        public bool IsOwned => OwnedByPlayerId.HasValue;
    }

    [Table("Package")]
    public class PackageData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Size { get; set; } // S, M, L, XL
        public float Weight { get; set; } = 1f;
        [Indexed]
        public int OriginTileId { get; set; }
        [Indexed]
        public int DestinationTileId { get; set; }
        [Indexed]
        public string Status { get; set; } = "incoming"; // incoming, packaged, loaded, in_transit, delivered
        public int TimeLimit { get; set; } = 300;
        public float BaseValue { get; set; } = 50f;
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
    }

    [Table("Delivery")]
    public class DeliveryData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int PackageId { get; set; }
        [Indexed]
        public int PlayerId { get; set; }
        public int VehicleId { get; set; }
        public float TimeScore { get; set; } = 0f;
        public float ZoneBonus { get; set; } = 0f;
        public float TotalScore { get; set; } = 0f;
        public float Payout { get; set; } = 0f;
        public string CompletedAt { get; set; } = DateTime.Now.ToString("o");
    }

    [Table("TransactionLog")]
    public class TransactionData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int PlayerId { get; set; }
        [Indexed]
        public string Type { get; set; } // delivery, tile_buy, tile_sell, vehicle_buy, etc.
        public float Amount { get; set; }
        public int? RelatedId { get; set; }
        public string Description { get; set; }
        public string Timestamp { get; set; } = DateTime.Now.ToString("o");
    }
}
