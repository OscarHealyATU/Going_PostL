using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(gridify))]
public class WarehouseLocations : MonoBehaviour
{
    [Header("Zone Identity")]
    [Tooltip("Must match the zoneName string stored in the Warehouse DB rows for this zone.")]
    public string zoneName = "Zone 1";

    [Header("Debug Override")]
    [Tooltip("If true, ignore the DB and place a single warehouse at the coords below.")]
    public bool useDebugSingleton = false;
    public int debugTileX = 12;
    public int debugTileZ = 12;

    public bool[,] warehouseLocations;
    [HideInInspector] public bool isReady = false;

    private gridify sourceGrid;

    void Awake()
    {
        sourceGrid = GetComponent<gridify>();
        StartCoroutine(LoadWhenReady());
    }

    private IEnumerator LoadWhenReady()
    {
        yield return new WaitUntil(() =>
            DbBoot.Instance != null &&
            DbBoot.Instance.Db != null);

        int gridW = (int)sourceGrid.noOfHousesX;
        int gridH = (int)sourceGrid.noOfHousesZ;
        warehouseLocations = new bool[gridW, gridH];

        if (useDebugSingleton)
        {
            if (debugTileX >= 0 && debugTileX < gridW && debugTileZ >= 0 && debugTileZ < gridH)
            {
                warehouseLocations[debugTileX, debugTileZ] = true;
                Debug.Log($"[WarehouseLocations:{zoneName}] DEBUG singleton at ({debugTileX}, {debugTileZ})");
            }
            isReady = true;
            yield break;
        }

        // Query the DB directly for warehouses in this zone.
        var rows = DbBoot.Instance.Db
            .Table<Warehouse>()
            .Where(w => w.zoneName == zoneName)
            .ToList();

        int count = 0;
        foreach (var w in rows)
        {
            if (w.tileX >= 0 && w.tileX < gridW && w.tileZ >= 0 && w.tileZ < gridH)
            {
                warehouseLocations[w.tileX, w.tileZ] = true;
                count++;
            }
        }

        //Debug.Log($"[WarehouseLocations:{zoneName}] Loaded {count}/{rows.Count} warehouses from DB");
        isReady = true;
    }

    public bool hasWarehouseAtLocation(int x, int z)
    {
        if (warehouseLocations == null) return false;
        if (x >= 0 && x < warehouseLocations.GetLength(0) && z >= 0 && z < warehouseLocations.GetLength(1))
            return warehouseLocations[x, z];
        return false;
    }
}