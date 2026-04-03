using UnityEngine;

[ExecuteInEditMode]
public class WarehousesGridify : MonoBehaviour
{
    [Header("Warehouse Handling")]
    public bool hasWarehouse = false;
    public WarehouseLocations warehouseLocations;
    public GameObject warehouseMWallPrefab, warehouseLWallPrefab, warehouseLWallFlippedPrefab;
    public int warehouseXPosition = 0, warehouseZPosition = 0;
  
    [Header("Grid Positioning")]
    public int xStartPosition, zStartPosition;

    [Header("Grid Size & Spacing")]
    public int noOfHousesX = 25, noOfHousesZ = 25, distance = 22;
    public Vector3 houseScale = new Vector3(500f, 500f, 500f);
    // void Start()
    [ContextMenu("Preview Walls")]
    public void PreviewWalls()
    {
        warehouseLocations.initWarehouseLocations();
        ClearWalls();
        for (int x = 0; x < noOfHousesX; x++)
        {
            for (int z = 0; z < noOfHousesZ; z++)
            {
                if (!warehouseLocations.hasWarehouseAtLocation(x, z)) continue;

                Vector3 position = new Vector3(
                    xStartPosition + x * distance,
                    0,
                    zStartPosition + z * distance);

                // bool isFlipped =true;     

                bool hasLeftNeighbor   = warehouseLocations.hasWarehouseAtLocation(x - 1, z);
                bool hasRightNeighbor  = warehouseLocations.hasWarehouseAtLocation(x + 1, z);

                bool hasBottomNeighbor = warehouseLocations.hasWarehouseAtLocation(x, z - 1);
                bool hasTopNeighbor    = warehouseLocations.hasWarehouseAtLocation(x, z + 1);

                if (!hasLeftNeighbor)   PlaceWall(position, 270f, hasBottomNeighbor || hasTopNeighbor, false); 
                if (!hasRightNeighbor)  PlaceWall(position,  90f, hasBottomNeighbor || hasTopNeighbor, false); 

                if (!hasBottomNeighbor) PlaceWall(position, 180f, hasLeftNeighbor   || hasRightNeighbor, false); 
                if (!hasTopNeighbor)    PlaceWall(position,   0f, hasLeftNeighbor   || hasRightNeighbor, true);
              
                // if (!warehouseLocations.hasWarehouseAtLocation(x - 1, z)) PlaceWall(position, 270f);
                // if (!warehouseLocations.hasWarehouseAtLocation(x + 1, z)) PlaceWall(position,  90f);
                // if (!warehouseLocations.hasWarehouseAtLocation(x, z - 1)) PlaceWall(position, 180f);
                // if (!warehouseLocations.hasWarehouseAtLocation(x, z + 1)) PlaceWall(position,   0f);
            }
        }
    }
    public void PlaceWall(Vector3 position, float rotationY, bool isLWall, bool isFlipped)
    {
        // Choose the appropriate wall prefab based on whether it's an L-wall or M-wall
        // GameObject wallPrefab = isLWall ? (isFlipped ? warehouseLWallFlippedPrefab : warehouseLWallPrefab) : warehouseMWallPrefab;
        GameObject wall = Instantiate(warehouseMWallPrefab, position, Quaternion.Euler(-90, rotationY, 0), transform);
        wall.transform.localScale = houseScale;
    }
    [ContextMenu("Clear Walls")]
    public void ClearWalls()
    {
        // Destroy all child objects (walls) of this GameObject
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }


}
