using UnityEngine;

public class WarehousesGridify : MonoBehaviour
{
    [Header("Warehouse Handling")]
    public bool hasWarehouse = false;
    public WarehouseLocations warehouseLocations;
    public GameObject warehouseWallPrefab;
    public int warehouseXPosition = 0;
    public int warehouseZPosition = 0;

    [Header("Grid Positioning")]
    public int xStartPosition, zStartPosition;

    [Header("Grid Size & Spacing")]
    public int noOfHousesX = 25, noOfHousesZ = 25, distance = 22;
    public Vector3 houseScale = new Vector3(500f, 500f, 500f);
    void Start()
    {
        for (int x = 0; x < noOfHousesX; x++)
        {
            for (int z = 0; z < noOfHousesZ; z++)
            {
                if (!warehouseLocations.hasWarehouseAtLocation(x, z)) continue;

                Vector3 position = new Vector3(
                    xStartPosition + x * distance,
                    0,
                    zStartPosition + z * distance);

                if (!warehouseLocations.hasWarehouseAtLocation(x - 1, z)) PlaceWall(position, 270f);
                if (!warehouseLocations.hasWarehouseAtLocation(x + 1, z)) PlaceWall(position,  90f);
                if (!warehouseLocations.hasWarehouseAtLocation(x, z - 1)) PlaceWall(position, 180f);
                if (!warehouseLocations.hasWarehouseAtLocation(x, z + 1)) PlaceWall(position,   0f);
            }
        }
    }
    public void PlaceWall(Vector3 position, float rotationY)
    {
        GameObject wall = Instantiate(warehouseWallPrefab, position, Quaternion.Euler(-90, rotationY, 0), transform);
        wall.transform.localScale = houseScale;
    }


}
