using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterWarehouseRegistrar : MonoBehaviour
{
    [Header("Zone Grids")]
    [SerializeField] private List<WarehouseZoneGrid> zoneGrids = new List<WarehouseZoneGrid>();

    [Header("Starter Warehouse")]
    [SerializeField] private string zoneName = "Zone 1";
    [SerializeField] private int starterTileX = 0;
    [SerializeField] private int starterTileZ = 0;

    [Header("Optional Runtime Lookup")]
    [SerializeField] private bool tryFindRuntimeWarehouseObject = false;
    [SerializeField] private string runtimeWarehouseObjectName = "purchased_Warehouse1(Clone)";
    [SerializeField] private float lookupDelay = 0.25f;

    [Header("Behaviour")]
    [SerializeField] private bool registerOnStart = true;
    [SerializeField] private bool setAsCurrentWarehouseAfterRegister = true;

    private void Start()
    {
        if (!registerOnStart)
            return;

        if (tryFindRuntimeWarehouseObject)
            StartCoroutine(RegisterAfterLookupDelay());
        else
            RegisterStarterWarehouseFromTile();
    }

    private IEnumerator RegisterAfterLookupDelay()
    {
        yield return new WaitForSeconds(lookupDelay);
        RegisterStarterWarehouseFromRuntimeObjectOrTile();
    }

    [ContextMenu("Register Starter Warehouse From Tile")]
    public void RegisterStarterWarehouseFromTile()
    {
        gridify zoneGrid = GetGridForZone(zoneName);
        if (zoneGrid == null)
        {
            Debug.LogWarning("[StarterWarehouseRegistrar] No grid found for zone: " + zoneName);
            return;
        }

        Vector3 worldPos = WarehouseService.TileToWorld(zoneGrid, starterTileX, starterTileZ);

        WarehouseService.EnsureStarterWarehouse(
            zoneName,
            starterTileX,
            starterTileZ,
            worldPos.x,
            worldPos.y,
            worldPos.z
        );

        if (setAsCurrentWarehouseAfterRegister)
            WarehouseService.SetLastInteractedWarehouse(zoneName, starterTileX, starterTileZ);
    }

    [ContextMenu("Register Starter Warehouse From Runtime Object Or Tile")]
    public void RegisterStarterWarehouseFromRuntimeObjectOrTile()
    {
        gridify zoneGrid = GetGridForZone(zoneName);
        if (zoneGrid == null)
        {
            Debug.LogWarning("[StarterWarehouseRegistrar] No grid found for zone: " + zoneName);
            return;
        }

        GameObject runtimeWarehouse = null;

        if (!string.IsNullOrWhiteSpace(runtimeWarehouseObjectName))
            runtimeWarehouse = GameObject.Find(runtimeWarehouseObjectName);

        if (runtimeWarehouse != null)
        {
            int tileX;
            int tileZ;

            if (WarehouseService.TryWorldToTile(zoneGrid, runtimeWarehouse.transform.position, out tileX, out tileZ))
            {
                Vector3 worldPos = runtimeWarehouse.transform.position;

                WarehouseService.EnsureStarterWarehouse(
                    zoneName,
                    tileX,
                    tileZ,
                    worldPos.x,
                    worldPos.y,
                    worldPos.z
                );

                if (setAsCurrentWarehouseAfterRegister)
                    WarehouseService.SetLastInteractedWarehouse(zoneName, tileX, tileZ);

                Debug.Log($"[StarterWarehouseRegistrar] Registered starter warehouse from runtime object at tile ({tileX}, {tileZ})");
                return;
            }
        }

        RegisterStarterWarehouseFromTile();
    }

    private gridify GetGridForZone(string targetZoneName)
    {
        for (int i = 0; i < zoneGrids.Count; i++)
        {
            WarehouseZoneGrid entry = zoneGrids[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.zoneName))
                continue;

            if (string.Equals(entry.zoneName, targetZoneName, StringComparison.OrdinalIgnoreCase))
                return entry.grid;
        }

        return null;
    }
}