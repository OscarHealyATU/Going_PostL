using System.Collections.Generic;
using UnityEngine;

public class DeliveryGridProvider : MonoBehaviour
{
    public static DeliveryGridProvider Instance { get; private set; }

    [Header("Match these to your city generator")]
    public float xStartPosition;
    public float zStartPosition;
    public int noOfHousesX = 25;
    public int noOfHousesZ = 25;
    public float distance = 22f;

    [Header("Delivery placement")]
    public float pointYOffset = 0f;
    public float edgePadding = 2f;

    [Header("Optional")]
    public bool avoidCenterArea = true;
    public Vector3 center = Vector3.zero;
    public float avoidRadius = 40f;

    private readonly List<Vector3> cachedPoints = new List<Vector3>();

    private void Awake()
    {
        Instance = this;
        BuildPoints();
    }

    private void BuildPoints()
    {
        cachedPoints.Clear();

        for (int x = 0; x < noOfHousesX; x++)
        {
            for (int z = 0; z < noOfHousesZ; z++)
            {
                float baseX = xStartPosition + x * distance;
                float baseZ = zStartPosition + z * distance;

                Vector3 point = new Vector3(
                    baseX + Random.Range(-edgePadding, edgePadding),
                    pointYOffset,
                    baseZ + Random.Range(-edgePadding, edgePadding)
                );

                if (avoidCenterArea && Vector3.Distance(point, center) < avoidRadius)
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
            Debug.LogWarning("DeliveryGridProvider: no delivery points available.");
            return Vector3.zero;
        }

        return cachedPoints[Random.Range(0, cachedPoints.Count)];
    }
}