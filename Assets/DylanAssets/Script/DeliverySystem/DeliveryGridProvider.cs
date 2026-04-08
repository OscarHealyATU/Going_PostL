using System.Collections.Generic;
using UnityEngine;

public class DeliveryGridProvider : MonoBehaviour
{
    public static DeliveryGridProvider Instance { get; private set; }

    [System.Serializable]
    public class ZoneGridEntry
    {
        [Tooltip("The zone bucket these delivery points belong to.")]
        public int zoneId = 1;

        [Tooltip("Which purchased zone unlocks this grid.")]
        public int unlockWithZoneId = 1;

        [Tooltip("Optional scene grid reference. Useful in Main scene.")]
        public gridify grid;

        [Tooltip("Optional shared asset layout. Useful in Warehouse scene where gridify objects do not exist.")]
        public DeliveryZoneLayoutAsset layoutAsset;
    }

    [Header("Source Grids")]
    [Tooltip("Each entry can use either a scene gridify reference, or a shared layout asset, or both.")]
    [SerializeField] private ZoneGridEntry[] zoneGrids;

    [Header("Legacy Single Grid Fallback")]
    [Tooltip("Used only if Zone Grids is empty.")]
    [SerializeField] private gridify cityGrid;

    [Header("Fallback (used only if no grid or asset is assigned)")]
    public float xStartPosition;
    public float zStartPosition;
    public int noOfHousesX = 25;
    public int noOfHousesZ = 25;
    public float distance = 22f;

    [Header("Delivery placement")]
    public float pointYOffset = 0f;
    public float forwardOffset = 7f;
    public float sidewaysJitter = 2f;

    [Header("Optional")]
    public bool avoidCenterArea = true;
    public Vector3 center = Vector3.zero;
    public float avoidRadius = 40f;

    [Header("Zone Setup")]
    [Tooltip("How many delivery zones the map is divided into when using the legacy single-grid mode.")]
    [SerializeField] private int zoneCount = 6;

    [Tooltip("If true, zones are split along Z. If false, zones are split along X. Used in legacy single-grid mode.")]
    [SerializeField] private bool splitAlongZ = true;

    [Header("Debug")]
    public bool rebuildOnStart = true;
    public bool drawGizmos = false;
    public float gizmoSphereSize = 1.5f;

    private readonly List<Vector3> cachedPoints = new List<Vector3>();
    private readonly Dictionary<int, List<Vector3>> cachedPointsByZone = new Dictionary<int, List<Vector3>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cityGrid == null && (zoneGrids == null || zoneGrids.Length == 0))
            cityGrid = FindFirstObjectByType<gridify>();

        if (rebuildOnStart)
            BuildPoints();
    }

    public void BuildPoints()
    {
        cachedPoints.Clear();
        cachedPointsByZone.Clear();

        bool builtFromZoneGrids = BuildFromAssignedZoneGrids();

        if (!builtFromZoneGrids)
            BuildFromLegacySingleGrid();

        Debug.Log($"DeliveryGridProvider built {cachedPoints.Count} delivery points.");

        foreach (var kvp in cachedPointsByZone)
        {
            int count = kvp.Value != null ? kvp.Value.Count : 0;
            Debug.Log($"DeliveryGridProvider Zone {kvp.Key}: {count} points");
        }
    }

    private bool BuildFromAssignedZoneGrids()
    {
        if (zoneGrids == null || zoneGrids.Length == 0)
            return false;

        bool addedAny = false;

        for (int i = 0; i < zoneGrids.Length; i++)
        {
            ZoneGridEntry entry = zoneGrids[i];
            if (entry == null)
                continue;

            int zoneId = Mathf.Max(1, entry.zoneId);
            int unlockWithZoneId = Mathf.Max(1, entry.unlockWithZoneId);

            if (!IsZoneAvailable(unlockWithZoneId))
                continue;

            if (!cachedPointsByZone.ContainsKey(zoneId))
                cachedPointsByZone[zoneId] = new List<Vector3>();

            bool addedFromThisEntry = false;

            if (entry.grid != null)
            {
                AddPointsFromGrid(entry.grid, zoneId);
                addedFromThisEntry = true;
            }
            else if (entry.layoutAsset != null)
            {
                AddPointsFromLayout(entry.layoutAsset, zoneId);
                addedFromThisEntry = true;
            }

            if (addedFromThisEntry)
                addedAny = true;
        }

        return addedAny;
    }

    private void BuildFromLegacySingleGrid()
    {
        int safeZoneCount = Mathf.Max(1, zoneCount);

        for (int i = 1; i <= safeZoneCount; i++)
        {
            if (IsZoneAvailable(i) && !cachedPointsByZone.ContainsKey(i))
                cachedPointsByZone[i] = new List<Vector3>();
        }

        float startX;
        float startZ;
        int countX;
        int countZ;
        float spacing;

        if (cityGrid != null)
        {
            startX = cityGrid.xStartPosition;
            startZ = cityGrid.zStartPosition;
            countX = Mathf.RoundToInt(cityGrid.noOfHousesX);
            countZ = Mathf.RoundToInt(cityGrid.noOfHousesZ);
            spacing = cityGrid.distance;
        }
        else
        {
            Debug.LogWarning("DeliveryGridProvider: no assigned zone grids and no cityGrid found, using fallback values.");

            startX = xStartPosition;
            startZ = zStartPosition;
            countX = noOfHousesX;
            countZ = noOfHousesZ;
            spacing = distance;
        }

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                int zoneId = GetZoneIdForCell(x, z, countX, countZ, safeZoneCount);

                if (!IsZoneAvailable(zoneId))
                    continue;

                Vector3 houseCenter = new Vector3(
                    startX + x * spacing,
                    pointYOffset,
                    startZ + z * spacing
                );

                if (TryCreateDeliveryPoint(houseCenter, out Vector3 point))
                {
                    cachedPoints.Add(point);

                    if (!cachedPointsByZone.ContainsKey(zoneId))
                        cachedPointsByZone[zoneId] = new List<Vector3>();

                    cachedPointsByZone[zoneId].Add(point);
                }
            }
        }
    }

    private void AddPointsFromGrid(gridify sourceGrid, int zoneId)
    {
        int countX = Mathf.RoundToInt(sourceGrid.noOfHousesX);
        int countZ = Mathf.RoundToInt(sourceGrid.noOfHousesZ);
        float startX = sourceGrid.xStartPosition;
        float startZ = sourceGrid.zStartPosition;
        float spacing = sourceGrid.distance;

        AddPointsFromRawData(zoneId, startX, startZ, countX, countZ, spacing);
    }

    private void AddPointsFromLayout(DeliveryZoneLayoutAsset layout, int zoneId)
    {
        if (layout == null)
            return;

        AddPointsFromRawData(
            zoneId,
            layout.xStartPosition,
            layout.zStartPosition,
            layout.noOfHousesX,
            layout.noOfHousesZ,
            layout.distance
        );
    }

    private void AddPointsFromRawData(int zoneId, float startX, float startZ, int countX, int countZ, float spacing)
    {
        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                Vector3 houseCenter = new Vector3(
                    startX + x * spacing,
                    pointYOffset,
                    startZ + z * spacing
                );

                if (TryCreateDeliveryPoint(houseCenter, out Vector3 point))
                {
                    cachedPoints.Add(point);

                    if (!cachedPointsByZone.ContainsKey(zoneId))
                        cachedPointsByZone[zoneId] = new List<Vector3>();

                    cachedPointsByZone[zoneId].Add(point);
                }
            }
        }
    }

    private bool TryCreateDeliveryPoint(Vector3 houseCenter, out Vector3 point)
    {
        int side = Random.Range(0, 4);

        Vector3 offsetDir;
        Vector3 sideDir;

        switch (side)
        {
            case 0:
                offsetDir = Vector3.forward;
                sideDir = Vector3.right;
                break;
            case 1:
                offsetDir = Vector3.back;
                sideDir = Vector3.right;
                break;
            case 2:
                offsetDir = Vector3.right;
                sideDir = Vector3.forward;
                break;
            default:
                offsetDir = Vector3.left;
                sideDir = Vector3.forward;
                break;
        }

        float jitter = Random.Range(-sidewaysJitter, sidewaysJitter);

        point = houseCenter
              + offsetDir * forwardOffset
              + sideDir * jitter;

        point.y = pointYOffset;

        if (avoidCenterArea)
        {
            Vector3 flatPoint = new Vector3(point.x, 0f, point.z);
            Vector3 flatCenter = new Vector3(center.x, 0f, center.z);

            if (Vector3.Distance(flatPoint, flatCenter) < avoidRadius)
                return false;
        }

        return true;
    }

    private bool IsZoneAvailable(int zoneId)
    {
        if (zoneId <= 1)
            return true;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        return ZoneService.IsZoneUnlocked(zoneId);
    }

    public void RefreshUnlockedZones()
    {
        BuildPoints();
    }

    public Vector3 GetRandomPoint()
    {
        if (cachedPoints.Count == 0)
        {
            Debug.LogWarning("DeliveryGridProvider: no delivery points available. Rebuilding now.");
            BuildPoints();

            if (cachedPoints.Count == 0)
                return Vector3.zero;
        }

        return cachedPoints[Random.Range(0, cachedPoints.Count)];
    }

    public Vector3 GetRandomPointInZone(int zoneId)
    {
        if (cachedPoints.Count == 0 || cachedPointsByZone.Count == 0)
        {
            Debug.LogWarning("DeliveryGridProvider: no cached zone points available. Rebuilding now.");
            BuildPoints();
        }

        int safeZoneId = Mathf.Max(1, zoneId);

        if (cachedPointsByZone.TryGetValue(safeZoneId, out List<Vector3> zonePoints))
        {
            if (zonePoints != null && zonePoints.Count > 0)
                return zonePoints[Random.Range(0, zonePoints.Count)];
        }

        Debug.LogWarning($"DeliveryGridProvider: zone {safeZoneId} has no points. Falling back to any available zone.");
        return GetRandomPoint();
    }

    public Vector3 GetRandomPointForZone(int zoneId)
    {
        return GetRandomPointInZone(zoneId);
    }

    private int GetZoneIdForCell(int x, int z, int countX, int countZ, int safeZoneCount)
    {
        if (safeZoneCount <= 1)
            return 1;

        if (splitAlongZ)
        {
            float normalized = countZ <= 1 ? 0f : z / (float)countZ;
            int zoneIndex = Mathf.FloorToInt(normalized * safeZoneCount);
            zoneIndex = Mathf.Clamp(zoneIndex, 0, safeZoneCount - 1);
            return zoneIndex + 1;
        }
        else
        {
            float normalized = countX <= 1 ? 0f : x / (float)countX;
            int zoneIndex = Mathf.FloorToInt(normalized * safeZoneCount);
            zoneIndex = Mathf.Clamp(zoneIndex, 0, safeZoneCount - 1);
            return zoneIndex + 1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || cachedPoints == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < cachedPoints.Count; i++)
            Gizmos.DrawSphere(cachedPoints[i], gizmoSphereSize);
    }
}