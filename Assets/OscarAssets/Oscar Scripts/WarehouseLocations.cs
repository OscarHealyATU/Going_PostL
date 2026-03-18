using UnityEngine;

public class WarehouseLocations : MonoBehaviour
{
    public bool [,] warehouseLocations;
    public int gridX = 25;
    public int gridZ = 25;

    public void Awake()
    {
        warehouseLocations = new bool[gridX, gridZ];

    /*
    * test array values
    */
    // Test warehouses - remove later when SQLite is ready
    // 2x2 warehouse cluster at (5,5)
    SetWarehouseLocation(5, 5, true);
    SetWarehouseLocation(5, 6, true);
    SetWarehouseLocation(6, 5, true);
    SetWarehouseLocation(6, 6, true);
    
    // L-shaped warehouse at (12,10)
    SetWarehouseLocation(12, 10, true);
    SetWarehouseLocation(12, 11, true);
    SetWarehouseLocation(12, 12, true);
    SetWarehouseLocation(13, 10, true);
    
    // Single standalone warehouse
    SetWarehouseLocation(20, 20, true);
    }

    public void SetWarehouseLocation(int x, int z, bool hasWarehouse)
    {
        if (x >= 0 && x < gridX && z >= 0 && z < gridZ)
        {
            warehouseLocations[x, z] = hasWarehouse;
        }
    }
    public bool hasWarehouseAtLocation(int x, int z)
    {
        if (x >= 0 && x < gridX && z >= 0 && z < gridZ)
        {
            return warehouseLocations[x, z];
        }
        return false;
    }
}
