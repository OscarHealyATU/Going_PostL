using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class gridify : MonoBehaviour
{
    [Header("Warehouse Handling")]
    public bool hasWarehouse = false;
    public GameObject warehousePrefab;
    public int warehouseXPosition = 0;
    public int warehouseZPosition = 0;

    [Header("City Seed")]
    public int seed = 42;
    public bool randomizeSeed = false;
    [SerializeField] private int previousSeed;

    [Header("Buildings & Props")]
    public GameObject[] housePrefabs;
    public GameObject[] streetPropPrefabs;
    public GameObject groundSquare;

    [Header("Grid Positioning")]
    public float xStartPosition;
    public float zStartPosition;

    [Header("Grid Size & Spacing")]
    public float noOfHousesX = 25f;
    public float noOfHousesZ = 25f;
    public float distance = 22f;
    public Vector3 houseScale = new Vector3(500f, 500f, 500f);

    [Header("Road Layout")]
    [Min(2)] public int roadEveryN = 4;        // every Nth row/col becomes road
    [Min(1)] public int roadWidthCells = 1;    // widen roads (1 or 2 are common)
    public bool spawnPropsOnRoads = true;      // props on roads
    public bool spawnPropsOnBuildings = true;  // props on building cells

    // Exposed for traffic/pathfinding
    public bool[,] isRoad;

    private GameObject[,] houses;

    void Start()
    {
        previousSeed = randomizeSeed ? System.DateTime.Now.GetHashCode() : seed;
        Random.InitState(previousSeed);

        int sizeX = Mathf.FloorToInt(noOfHousesX);
        int sizeZ = Mathf.FloorToInt(noOfHousesZ);

        isRoad = new bool[sizeX, sizeZ];
        houses = new GameObject[sizeX, sizeZ];

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                Vector3 position = new Vector3(xStartPosition + x * distance, 0f, zStartPosition + z * distance);

                bool roadCell = IsRoadIndex(x) || IsRoadIndex(z);
                isRoad[x, z] = roadCell;

                // Tile
                if (groundSquare != null)
                {
                    var tile = Instantiate(groundSquare, position, Quaternion.Euler(-90, 0, 0), transform);
                    tile.transform.localScale = houseScale; // FIX: scale the instance, not the prefab ref
                }

                // Pick prefabs deterministically (keeps seed progression consistent)
                int propIndex = (streetPropPrefabs != null && streetPropPrefabs.Length > 0)
                    ? Random.Range(0, streetPropPrefabs.Length) : -1;

                int houseIndex = (housePrefabs != null && housePrefabs.Length > 0)
                    ? Random.Range(0, housePrefabs.Length) : -1;

                // Roads: no houses
                if (roadCell)
                {
                    if (spawnPropsOnRoads && propIndex >= 0)
                    {
                        var prop = Instantiate(streetPropPrefabs[propIndex], position, Quaternion.Euler(-90, 0, 0), transform);
                        prop.transform.localScale = houseScale;
                    }
                    continue;
                }

                // Buildings
                if (spawnPropsOnBuildings && propIndex >= 0)
                {
                    var prop = Instantiate(streetPropPrefabs[propIndex], position, Quaternion.Euler(-90, 0, 0), transform);
                    prop.transform.localScale = houseScale;
                }

                if (houseIndex >= 0)
                {
                    var house = Instantiate(housePrefabs[houseIndex], position, Quaternion.Euler(-90, 0, 0), transform);
                    house.transform.rotation = Quaternion.Euler(-90, 90 * Random.Range(0, 4), 0);
                    house.transform.localScale = houseScale * Random.Range(0.8f, 1.2f);

                    houses[x, z] = house;
                }
            }
        }

        // Warehouse replacement
        if (hasWarehouse && warehousePrefab != null)
        {
            // Clamp to avoid out-of-range
            warehouseXPosition = Mathf.Clamp(warehouseXPosition, 0, sizeX - 1);
            warehouseZPosition = Mathf.Clamp(warehouseZPosition, 0, sizeZ - 1);

            // If this cell is marked as road, you may want to force it to building
            isRoad[warehouseXPosition, warehouseZPosition] = false;

            if (houses[warehouseXPosition, warehouseZPosition] != null)
                Destroy(houses[warehouseXPosition, warehouseZPosition]); // FIX: only once

            Vector3 warehousePos = new Vector3(
                xStartPosition + warehouseXPosition * distance, 0f,
                zStartPosition + warehouseZPosition * distance
            );

            var warehouse = Instantiate(warehousePrefab, warehousePos, Quaternion.Euler(-90, 0, 0), transform);
            warehouse.transform.localScale = houseScale;
        }
    }

    bool IsRoadIndex(int i)
    {
        int m = Mod(i, roadEveryN);
        // roadWidthCells = 1 => exact multiples
        // roadWidthCells = 2 => multiples plus neighbor band
        return m < roadWidthCells || (roadEveryN - m) < roadWidthCells;
    }

    int Mod(int a, int b) => (a % b + b) % b;

#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    int previewSeed = randomizeSeed ? System.DateTime.Now.GetHashCode() : seed;
    UnityEngine.Random.InitState(previewSeed);

    int sx = Mathf.FloorToInt(noOfHousesX);
    int sz = Mathf.FloorToInt(noOfHousesZ);

    // --- 1) Draw tile cells (what spawns on each tile) ---
    for (int x = 0; x < sx; x++)
    {
        for (int z = 0; z < sz; z++)
        {
            Vector3 pos = new Vector3(xStartPosition + x * distance, 0f, zStartPosition + z * distance);

            bool roadCell = IsRoadIndex(x) || IsRoadIndex(z);
            bool isWarehouseCell = hasWarehouse && x == warehouseXPosition && z == warehouseZPosition;

            // Advance RNG same way Start() does (so labels match deterministic picks)
            int propIndex = (streetPropPrefabs != null && streetPropPrefabs.Length > 0)
                ? Random.Range(0, streetPropPrefabs.Length) : -1;

            int houseIndex = (housePrefabs != null && housePrefabs.Length > 0)
                ? Random.Range(0, housePrefabs.Length) : -1;

            // Color tiles by what they are (NOT "roads")
            // - Warehouse cell: blue-ish
            // - Road-reserved cell (no house): darker neutral
            // - Building cell: red-ish
            if (isWarehouseCell)
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
            else if (roadCell)
                Gizmos.color = new Color(0.15f, 0.15f, 0.15f, 0.18f);
            else
                Gizmos.color = new Color(1f, 0f, 0f, 0.12f);

            Gizmos.DrawCube(pos + Vector3.up * 0.05f, new Vector3(distance * 0.9f, 0.1f, distance * 0.9f));

            // Labels: describe contents, not "ROAD"
            if (isWarehouseCell)
                Handles.Label(pos + Vector3.up * 0.5f, "WAREHOUSE");
            else if (roadCell)
                Handles.Label(pos + Vector3.up * 0.5f, $"EMPTY P{propIndex}");
            else
                Handles.Label(pos + Vector3.up * 0.5f, $"H{houseIndex} P{propIndex}");
        }
    }

    // --- 2) Draw actual roads (the separators between tiles) as grid lines ---
    // We assume xStartPosition/zStartPosition are tile centers, so corners are half a tile away.
    Vector3 originCorner = new Vector3(xStartPosition - distance * 0.5f, 0f, zStartPosition - distance * 0.5f);

    // Vertical lines (constant X)
    Gizmos.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);
    for (int x = 0; x <= sx; x++)
    {
        float wx = originCorner.x + x * distance;
        Vector3 a = new Vector3(wx, 0.08f, originCorner.z);
        Vector3 b = new Vector3(wx, 0.08f, originCorner.z + sz * distance);
        Gizmos.DrawLine(a, b);
    }

    // Horizontal lines (constant Z)
    for (int z = 0; z <= sz; z++)
    {
        float wz = originCorner.z + z * distance;
        Vector3 a = new Vector3(originCorner.x, 0.08f, wz);
        Vector3 b = new Vector3(originCorner.x + sx * distance, 0.08f, wz);
        Gizmos.DrawLine(a, b);
    }
}
#endif
}