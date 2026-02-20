using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SQLite;

namespace DeliveryGame.Database
{
    /// <summary>
    /// High-level game service that coordinates database operations.
    /// Uses SQLite-net's ORM for cleaner queries.
    /// </summary>
    public class GameService : MonoBehaviour
    {
        public static GameService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==========================================
        // PLAYER OPERATIONS
        // ==========================================

        public PlayerData CreatePlayer(string name, float startingMoney = 5000f)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var player = new PlayerData
            {
                Name = name,
                Money = startingMoney
            };
            db.Insert(player);

            // Give starter vehicle (bicycle)
            var bicycle = db.Table<VehicleTypeData>().FirstOrDefault(v => v.Name == "Bicycle");
            if (bicycle != null)
            {
                db.Insert(new VehicleData
                {
                    VehicleTypeId = bicycle.Id,
                    OwnedByPlayerId = player.Id
                });
            }

            return player;
        }

        public PlayerData GetPlayer(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Find<PlayerData>(playerId);
        }

        public void AddMoney(int playerId, float amount)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var player = db.Find<PlayerData>(playerId);
            if (player != null)
            {
                player.Money += amount;
                db.Update(player);
            }
        }

        public void AddXp(int playerId, int xp)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var player = db.Find<PlayerData>(playerId);
            if (player != null)
            {
                player.Xp += xp;
                db.Update(player);
            }
        }

        // ==========================================
        // ZONE & TILE OPERATIONS
        // ==========================================

        public List<ZoneData> GetAllZones()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<ZoneData>().ToList();
        }

        public ZoneData GetZone(int zoneId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Find<ZoneData>(zoneId);
        }

        public int CreateTile(int zoneId, int gridX, int gridY, float purchasePrice, int customerCount)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var tile = new TileData
            {
                ZoneId = zoneId,
                GridX = gridX,
                GridY = gridY,
                PurchasePrice = purchasePrice,
                CustomerCount = customerCount
            };
            db.Insert(tile);
            return tile.Id;
        }

        public TileData GetTile(int tileId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Find<TileData>(tileId);
        }

        public List<TileData> GetUnownedTiles()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<TileData>()
                .Where(t => t.OwnedByPlayerId == null)
                .ToList();
        }

        public List<TileData> GetPlayerTiles(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<TileData>()
                .Where(t => t.OwnedByPlayerId == playerId)
                .ToList();
        }

        public List<TileData> GetTilesWithCustomers()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<TileData>()
                .Where(t => t.OwnedByPlayerId == null && t.CustomerCount > 0)
                .ToList();
        }

        public bool BuyTile(int playerId, int tileId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var player = db.Find<PlayerData>(playerId);
            var tile = db.Find<TileData>(tileId);

            if (player == null || tile == null) return false;
            if (tile.IsOwned) return false;
            if (player.Money < tile.PurchasePrice) return false;

            // Deduct money
            player.Money -= tile.PurchasePrice;
            db.Update(player);

            // Transfer ownership and remove customers
            tile.OwnedByPlayerId = playerId;
            tile.CustomerCount = 0;
            db.Update(tile);

            // Log transaction
            db.Insert(new TransactionData
            {
                PlayerId = playerId,
                Type = "tile_buy",
                Amount = -tile.PurchasePrice,
                RelatedId = tileId,
                Description = $"Purchased tile at ({tile.GridX}, {tile.GridY})"
            });

            return true;
        }

        public bool SellTile(int playerId, int tileId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var tile = db.Find<TileData>(tileId);
            if (tile == null || tile.OwnedByPlayerId != playerId) return false;

            var zone = db.Find<ZoneData>(tile.ZoneId);
            int restoredCustomers = zone?.BaseCustomerDensity ?? 5;

            // Sell at 75% of purchase price
            float sellPrice = tile.PurchasePrice * 0.75f;

            // Add money
            var player = db.Find<PlayerData>(playerId);
            player.Money += sellPrice;
            db.Update(player);

            // Remove ownership and restore customers
            tile.OwnedByPlayerId = null;
            tile.CustomerCount = restoredCustomers;
            db.Update(tile);

            // Log transaction
            db.Insert(new TransactionData
            {
                PlayerId = playerId,
                Type = "tile_sell",
                Amount = sellPrice,
                RelatedId = tileId,
                Description = $"Sold tile at ({tile.GridX}, {tile.GridY})"
            });

            return true;
        }

        // ==========================================
        // VEHICLE OPERATIONS
        // ==========================================

        public List<VehicleTypeData> GetAllVehicleTypes()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<VehicleTypeData>().OrderBy(v => v.BaseCost).ToList();
        }

        public List<VehicleData> GetPlayerVehicles(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<VehicleData>()
                .Where(v => v.OwnedByPlayerId == playerId)
                .ToList();
        }

        public bool BuyVehicle(int playerId, int vehicleTypeId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var player = db.Find<PlayerData>(playerId);
            var vehicleType = db.Find<VehicleTypeData>(vehicleTypeId);

            if (player == null || vehicleType == null) return false;
            if (player.Money < vehicleType.BaseCost) return false;

            // Deduct money
            player.Money -= vehicleType.BaseCost;
            db.Update(player);

            // Create vehicle
            var vehicle = new VehicleData
            {
                VehicleTypeId = vehicleTypeId,
                OwnedByPlayerId = playerId
            };
            db.Insert(vehicle);

            // Log transaction
            db.Insert(new TransactionData
            {
                PlayerId = playerId,
                Type = "vehicle_buy",
                Amount = -vehicleType.BaseCost,
                RelatedId = vehicle.Id,
                Description = $"Purchased {vehicleType.Name}"
            });

            return true;
        }

        public void UseFuel(int vehicleId, float amount)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var vehicle = db.Find<VehicleData>(vehicleId);
            if (vehicle != null)
            {
                vehicle.CurrentFuel = Mathf.Max(0, vehicle.CurrentFuel - amount);
                db.Update(vehicle);
            }
        }

        public void RefuelVehicle(int vehicleId, float amount)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var vehicle = db.Find<VehicleData>(vehicleId);
            if (vehicle != null)
            {
                vehicle.CurrentFuel = Mathf.Min(100, vehicle.CurrentFuel + amount);
                db.Update(vehicle);
            }
        }

        // ==========================================
        // EMPLOYEE OPERATIONS
        // ==========================================

        public List<EmployeeRoleData> GetAllRoles()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<EmployeeRoleData>().ToList();
        }

        public List<EmployeeData> GetPlayerEmployees(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<EmployeeData>()
                .Where(e => e.PlayerId == playerId)
                .ToList();
        }

        public EmployeeData HireEmployee(int playerId, string roleName, string employeeName)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var role = db.Table<EmployeeRoleData>().FirstOrDefault(r => r.Name == roleName);
            if (role == null) return null;

            // Random efficiency for new hires
            float efficiency = Random.Range(40f, 80f);
            float salary = role.BaseSalary * (0.8f + (efficiency / 100f) * 0.4f);

            var employee = new EmployeeData
            {
                PlayerId = playerId,
                RoleId = role.Id,
                Name = employeeName,
                Salary = salary,
                Efficiency = efficiency
            };
            db.Insert(employee);

            return employee;
        }

        public void FireEmployee(int employeeId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            db.Delete<EmployeeData>(employeeId);
        }

        public float GetTotalSalaryCost(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<EmployeeData>()
                .Where(e => e.PlayerId == playerId)
                .ToList()
                .Sum(e => e.Salary);
        }

        // ==========================================
        // PACKAGE OPERATIONS
        // ==========================================

        public PackageData GeneratePackage()
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var availableTiles = db.Table<TileData>()
                .Where(t => t.OwnedByPlayerId == null && t.CustomerCount > 0)
                .ToList();

            if (availableTiles.Count < 2) return null;

            // Pick random origin and destination
            int originIndex = Random.Range(0, availableTiles.Count);
            var originTile = availableTiles[originIndex];
            availableTiles.RemoveAt(originIndex);
            var destTile = availableTiles[Random.Range(0, availableTiles.Count)];

            // Random package properties
            string[] sizes = { "S", "M", "L", "XL" };
            int sizeIndex = Random.Range(0, 4);
            string size = sizes[sizeIndex];

            float weight = sizeIndex switch
            {
                0 => Random.Range(0.5f, 2f),
                1 => Random.Range(2f, 10f),
                2 => Random.Range(10f, 30f),
                3 => Random.Range(30f, 100f),
                _ => 1f
            };

            int timeLimit = sizeIndex switch
            {
                0 => Random.Range(180, 300),
                1 => Random.Range(240, 420),
                2 => Random.Range(300, 600),
                3 => Random.Range(420, 900),
                _ => 300
            };

            float baseValue = sizeIndex switch
            {
                0 => Random.Range(20f, 40f),
                1 => Random.Range(40f, 80f),
                2 => Random.Range(80f, 150f),
                3 => Random.Range(150f, 300f),
                _ => 50f
            };

            var package = new PackageData
            {
                Size = size,
                Weight = weight,
                OriginTileId = originTile.Id,
                DestinationTileId = destTile.Id,
                TimeLimit = timeLimit,
                BaseValue = baseValue
            };
            db.Insert(package);

            return package;
        }

        public List<PackageData> GetPackagesByStatus(string status)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<PackageData>()
                .Where(p => p.Status == status)
                .ToList();
        }

        public void UpdatePackageStatus(int packageId, string newStatus)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var package = db.Find<PackageData>(packageId);
            if (package != null)
            {
                package.Status = newStatus;
                db.Update(package);
            }
        }

        public bool AdvancePackageStatus(int packageId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var package = db.Find<PackageData>(packageId);
            if (package == null) return false;

            string nextStatus = package.Status switch
            {
                "incoming" => "packaged",
                "packaged" => "loaded",
                "loaded" => "in_transit",
                "in_transit" => "delivered",
                _ => package.Status
            };

            if (nextStatus != package.Status)
            {
                package.Status = nextStatus;
                db.Update(package);
                return true;
            }

            return false;
        }

        // ==========================================
        // DELIVERY OPERATIONS
        // ==========================================

        public DeliveryData CompleteDelivery(int packageId, int playerId, int vehicleId, float deliveryTimeSeconds)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            var package = db.Find<PackageData>(packageId);
            if (package == null) return null;

            var destTile = db.Find<TileData>(package.DestinationTileId);
            var zone = db.Find<ZoneData>(destTile.ZoneId);

            // Calculate time score
            float timeRatio = 1f - Mathf.Clamp01(deliveryTimeSeconds / package.TimeLimit);
            float timeScore = timeRatio * 100f;

            // Zone bonus
            float zoneBonus = (zone.BonusMultiplier - 0.8f) * 50f;

            // Total score
            float totalScore = Mathf.Clamp(timeScore + zoneBonus, 0f, 100f);

            // Calculate payout
            float payout = package.BaseValue * (totalScore / 100f) * zone.BonusMultiplier;

            // Mark package as delivered
            package.Status = "delivered";
            db.Update(package);

            // Create delivery record
            var delivery = new DeliveryData
            {
                PackageId = packageId,
                PlayerId = playerId,
                VehicleId = vehicleId,
                TimeScore = timeScore,
                ZoneBonus = zoneBonus,
                TotalScore = totalScore,
                Payout = payout
            };
            db.Insert(delivery);

            // Add money and XP to player
            var player = db.Find<PlayerData>(playerId);
            player.Money += payout;
            player.Xp += Mathf.RoundToInt(totalScore / 10f);
            db.Update(player);

            // Log transaction
            db.Insert(new TransactionData
            {
                PlayerId = playerId,
                Type = "delivery",
                Amount = payout,
                RelatedId = delivery.Id,
                Description = $"Delivery completed - Score: {totalScore:F0}"
            });

            return delivery;
        }

        public List<DeliveryData> GetPlayerDeliveries(int playerId, int limit = 50)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<DeliveryData>()
                .Where(d => d.PlayerId == playerId)
                .OrderByDescending(d => d.CompletedAt)
                .Take(limit)
                .ToList();
        }

        public int GetDeliveryCount(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<DeliveryData>()
                .Where(d => d.PlayerId == playerId)
                .Count();
        }

        public float GetTotalEarnings(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<DeliveryData>()
                .Where(d => d.PlayerId == playerId)
                .ToList()
                .Sum(d => d.Payout);
        }

        // ==========================================
        // SALARY & TRANSACTIONS
        // ==========================================

        public void PaySalaries(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            
            float totalSalary = db.Table<EmployeeData>()
                .Where(e => e.PlayerId == playerId)
                .ToList()
                .Sum(e => e.Salary);

            if (totalSalary > 0)
            {
                var player = db.Find<PlayerData>(playerId);
                player.Money -= totalSalary;
                db.Update(player);

                db.Insert(new TransactionData
                {
                    PlayerId = playerId,
                    Type = "salary",
                    Amount = -totalSalary,
                    Description = "Daily salary payments"
                });
            }
        }

        public List<TransactionData> GetPlayerTransactions(int playerId, int limit = 100)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            return db.Table<TransactionData>()
                .Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.Timestamp)
                .Take(limit)
                .ToList();
        }

        public (float income, float expenses) GetFinancialSummary(int playerId)
        {
            using var db = DatabaseManager.Instance.GetConnection();
            var transactions = db.Table<TransactionData>()
                .Where(t => t.PlayerId == playerId)
                .ToList();

            float income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            float expenses = transactions.Where(t => t.Amount < 0).Sum(t => Mathf.Abs(t.Amount));

            return (income, expenses);
        }
    }
}
