using System.Collections;
using UnityEngine;

[RequireComponent(typeof(WarehouseLocations))]
public class InteriorWarehouses : MonoBehaviour
{
    [Header("Wall Prefabs")]
    public GameObject warehouseMediumWallPrefab;
    public GameObject warehouseLargeWallExtendLeftPrefab;
    public GameObject warehouseLargeWallExtendRightPrefab;
    public GameObject warehouseDoubleExtendWallPrefab;
    public GameObject warehouseInsideCornerPrefab;

    [Header("Roof")]
    public GameObject warehouseRoofPrefab;
    public float roofRotationY = 0f;

    private WarehouseLocations warehouseLocations;
    private gridify sourceGrid;

    void Start()
    {
        warehouseLocations = GetComponent<WarehouseLocations>();
        sourceGrid = GetComponent<gridify>();
        StartCoroutine(BuildWhenReady());
    }

    private IEnumerator BuildWhenReady()
    {
        yield return new WaitUntil(() => warehouseLocations.isReady);
        BuildWalls();
    }

    private void BuildWalls()
    {
        int gridW = (int)sourceGrid.noOfHousesX;
        int gridH = (int)sourceGrid.noOfHousesZ;
        Vector3 scale = sourceGrid.houseScale;

        for (int x = 0; x < gridW; x++)
        {
            for (int z = 0; z < gridH; z++)
            {
                if (!warehouseLocations.hasWarehouseAtLocation(x, z)) continue;

                Vector3 position = new Vector3(
                    sourceGrid.xStartPosition + x * sourceGrid.distance,
                    0,
                    sourceGrid.zStartPosition + z * sourceGrid.distance
                );

                bool left   = warehouseLocations.hasWarehouseAtLocation(x - 1, z);
                bool right  = warehouseLocations.hasWarehouseAtLocation(x + 1, z);
                bool bottom = warehouseLocations.hasWarehouseAtLocation(x, z - 1);
                bool top    = warehouseLocations.hasWarehouseAtLocation(x, z + 1);

                // Perimeter walls
                if (!top)    PlaceSide(position,   0f, left,   right,  scale);
                if (!right)  PlaceSide(position,  90f, top,    bottom, scale);
                if (!bottom) PlaceSide(position, 180f, right,  left,   scale);
                if (!left)   PlaceSide(position, 270f, bottom, top,    scale);

                // Inside corners
                TryInsideCorner(position, top,    right,  x + 1, z + 1,  90f, scale);
                TryInsideCorner(position, right,  bottom, x + 1, z - 1, 180f, scale);
                TryInsideCorner(position, bottom, left,   x - 1, z - 1, 270f, scale);
                TryInsideCorner(position, left,   top,    x - 1, z + 1,   0f, scale);

                // Roof — only if the tile has no exterior walls (fully surrounded by warehouse neighbors)
                if (top && right && bottom && left)
                {
                    PlaceRoof(position, scale);
                }
            }
        }
    }

    private void PlaceRoof(Vector3 position, Vector3 scale)
    {
        if (warehouseRoofPrefab == null) return;

        GameObject roof = Instantiate(warehouseRoofPrefab, position, Quaternion.Euler(-90, roofRotationY, 0), transform);
        roof.transform.localScale = scale;
    }

    private void TryInsideCorner(Vector3 position, bool sideA, bool sideB, int diagX, int diagZ, float rotationY, Vector3 scale)
    {
        if (warehouseInsideCornerPrefab == null) return;
        if (!sideA || !sideB) return;
        if (warehouseLocations.hasWarehouseAtLocation(diagX, diagZ)) return;

        GameObject corner = Instantiate(warehouseInsideCornerPrefab, position, Quaternion.Euler(-90, rotationY, 0), transform);
        corner.transform.localScale = scale;
    }

    private void PlaceSide(Vector3 position, float rotationY, bool extendLeft, bool extendRight, Vector3 scale)
    {
        GameObject prefab;
        if (extendLeft && extendRight)   prefab = warehouseDoubleExtendWallPrefab != null ? warehouseDoubleExtendWallPrefab : warehouseMediumWallPrefab;
        else if (extendLeft)             prefab = warehouseLargeWallExtendLeftPrefab;
        else if (extendRight)            prefab = warehouseLargeWallExtendRightPrefab;
        else                             prefab = warehouseMediumWallPrefab;

        if (prefab == null) return;

        GameObject wall = Instantiate(prefab, position, Quaternion.Euler(-90, rotationY, 0), transform);
        wall.transform.localScale = scale;
    }
}