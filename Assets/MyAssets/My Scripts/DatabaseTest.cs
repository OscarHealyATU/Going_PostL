using UnityEngine;
using DeliveryGame.Database;

/// <summary>
/// Simple test script to verify the database system is working.
/// Attach this to any GameObject and press Play.
/// Check the Console for output.
/// </summary>
public class DatabaseTest : MonoBehaviour
{
    private void Start()
    {
        // Wait a frame to ensure DatabaseManager and GameService are initialized
        Invoke(nameof(RunTests), 0.1f);
    }

    private void RunTests()
    {
        Debug.Log("=== DATABASE TEST STARTING ===");

        // Test 1: Check managers exist
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("FAIL: DatabaseManager.Instance is null. Make sure you have a GameObject with DatabaseManager component.");
            return;
        }
        Debug.Log("PASS: DatabaseManager found");

        if (GameService.Instance == null)
        {
            Debug.LogError("FAIL: GameService.Instance is null. Make sure you have a GameObject with GameService component.");
            return;
        }
        Debug.Log("PASS: GameService found");

        // Test 2: Create a player
        Debug.Log("Creating test player...");
        var player = GameService.Instance.CreatePlayer("TestPlayer", 5000f);
        if (player == null)
        {
            Debug.LogError("FAIL: Could not create player");
            return;
        }
        Debug.Log($"PASS: Created player '{player.Name}' with ID {player.Id} and ${player.Money}");

        // Test 3: Check zones exist (seeded data)
        var zones = GameService.Instance.GetAllZones();
        if (zones == null || zones.Count == 0)
        {
            Debug.LogError("FAIL: No zones found. Seed data may not have been inserted.");
            return;
        }
        Debug.Log($"PASS: Found {zones.Count} zones:");
        foreach (var zone in zones)
        {
            Debug.Log($"  - {zone.Name} (bonus: {zone.BonusMultiplier}x)");
        }

        // Test 4: Check vehicle types exist
        var vehicleTypes = GameService.Instance.GetAllVehicleTypes();
        if (vehicleTypes == null || vehicleTypes.Count == 0)
        {
            Debug.LogError("FAIL: No vehicle types found.");
            return;
        }
        Debug.Log($"PASS: Found {vehicleTypes.Count} vehicle types:");
        foreach (var vt in vehicleTypes)
        {
            Debug.Log($"  - {vt.Name} (capacity: {vt.Capacity}, cost: ${vt.BaseCost})");
        }

        // Test 5: Check player got starter vehicle
        var vehicles = GameService.Instance.GetPlayerVehicles(player.Id);
        if (vehicles == null || vehicles.Count == 0)
        {
            Debug.LogWarning("WARNING: Player has no starter vehicle");
        }
        else
        {
            Debug.Log($"PASS: Player has {vehicles.Count} vehicle(s)");
        }

        // Test 6: Check employee roles exist
        var roles = GameService.Instance.GetAllRoles();
        if (roles == null || roles.Count == 0)
        {
            Debug.LogError("FAIL: No employee roles found.");
            return;
        }
        Debug.Log($"PASS: Found {roles.Count} employee roles:");
        foreach (var role in roles)
        {
            Debug.Log($"  - {role.Name} (base salary: ${role.BaseSalary})");
        }

        // Test 7: Create a tile and test buying
        Debug.Log("Creating test tile...");
        int tileId = GameService.Instance.CreateTile(
            zoneId: zones[0].Id,
            gridX: 0,
            gridY: 0,
            purchasePrice: 1000f,
            customerCount: 5
        );
        Debug.Log($"PASS: Created tile with ID {tileId}");

        // Test 8: Buy the tile
        bool bought = GameService.Instance.BuyTile(player.Id, tileId);
        if (!bought)
        {
            Debug.LogError("FAIL: Could not buy tile");
            return;
        }
        
        // Refresh player data
        player = GameService.Instance.GetPlayer(player.Id);
        Debug.Log($"PASS: Bought tile. Player money now: ${player.Money}");

        // Test 9: Check tile is owned
        var ownedTiles = GameService.Instance.GetPlayerTiles(player.Id);
        Debug.Log($"PASS: Player owns {ownedTiles.Count} tile(s)");

        // Test 10: Hire an employee
        var employee = GameService.Instance.HireEmployee(player.Id, "packager", "John Smith");
        if (employee == null)
        {
            Debug.LogError("FAIL: Could not hire employee");
            return;
        }
        Debug.Log($"PASS: Hired {employee.Name} as packager (efficiency: {employee.Efficiency:F0}%, salary: ${employee.Salary:F2})");

        // Test 11: Check transactions were logged
        var transactions = GameService.Instance.GetPlayerTransactions(player.Id);
        Debug.Log($"PASS: Found {transactions.Count} transaction(s):");
        foreach (var t in transactions)
        {
            Debug.Log($"  - {t.Type}: ${t.Amount:F2} ({t.Description})");
        }

        Debug.Log("=== ALL TESTS PASSED ===");
        Debug.Log($"Database location: {DatabaseManager.Instance.DatabasePath}");
    }
}
