using System.Collections.Generic;
using UnityEngine;

public class DeliveryGridProvider : MonoBehaviour
{
    public static DeliveryGridProvider Instance { get; private set; }

    [Header("Source Grid")]
    [SerializeField] private gridify cityGrid;

    [Header("Fallback (used only if cityGrid is missing)")]
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

    [Header("Debug")]
    public bool rebuildOnStart = true;
    public bool drawGizmos = false;
    public float gizmoSphereSize = 1.5f;

    private readonly List<Vector3> cachedPoints = new List<Vector3>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cityGrid == null)
            cityGrid = FindFirstObjectByType<gridify>();

        if (rebuildOnStart)
            BuildPoints();
    }

    public void BuildPoints()
    {
        cachedPoints.Clear();

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
            Debug.LogWarning("DeliveryGridProvider: gridify not found, using fallback values.");

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
                Vector3 houseCenter = new Vector3(
                    startX + x * spacing,
                    pointYOffset,
                    startZ + z * spacing
                );

                // Pick one of the 4 sides of the house/grid cell.
                int side = Random.Range(0, 4);

                Vector3 offsetDir;
                Vector3 sideDir;

                switch (side)
                {
                    case 0: // north
                        offsetDir = Vector3.forward;
                        sideDir = Vector3.right;
                        break;
                    case 1: // south
                        offsetDir = Vector3.back;
                        sideDir = Vector3.right;
                        break;
                    case 2: // east
                        offsetDir = Vector3.right;
                        sideDir = Vector3.forward;
                        break;
                    default: // west
                        offsetDir = Vector3.left;
                        sideDir = Vector3.forward;
                        break;
                }

                float jitter = Random.Range(-sidewaysJitter, sidewaysJitter);

                Vector3 point = houseCenter
                              + offsetDir * forwardOffset
                              + sideDir * jitter;

                point.y = pointYOffset;

                if (avoidCenterArea && Vector3.Distance(new Vector3(point.x, 0f, point.z), new Vector3(center.x, 0f, center.z)) < avoidRadius)
                    continue;

                cachedPoints.Add(point);
            }
        }

        Debug.Log($"DeliveryGridProvider built {cachedPoints.Count} delivery points.");
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

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || cachedPoints == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < cachedPoints.Count; i++)
            Gizmos.DrawSphere(cachedPoints[i], gizmoSphereSize);
    }
}