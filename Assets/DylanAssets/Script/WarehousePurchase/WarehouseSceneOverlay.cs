using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarehouseSceneOverlay : MonoBehaviour
{
    [Header("Zone Grids")]
    [SerializeField] private List<WarehouseZoneGrid> zoneGrids = new List<WarehouseZoneGrid>();

    [Header("Warehouse Prefab")]
    [SerializeField] private GameObject warehousePrefab;

    [Header("Behaviour")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private float positionTolerance = 0.35f;

    private readonly List<GameObject> spawnedWarehouses = new List<GameObject>();

    private bool hasBuilt;
    private Coroutine buildRoutine;

    private void Start()
    {
        if (buildOnStart)
            buildRoutine = StartCoroutine(BuildWhenReady());
    }

    private IEnumerator BuildWhenReady()
    {
        yield return new WaitUntil(IsReadyToBuild);
        RebuildWarehouses();
        hasBuilt = true;
        buildRoutine = null;
    }

    private bool IsReadyToBuild()
    {
        if (warehousePrefab == null)
            return false;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        if (zoneGrids == null || zoneGrids.Count == 0)
            return false;

        bool foundAtLeastOneValidGrid = false;

        for (int i = 0; i < zoneGrids.Count; i++)
        {
            WarehouseZoneGrid entry = zoneGrids[i];
            if (entry == null || entry.grid == null)
                continue;

            foundAtLeastOneValidGrid = true;

            // gridify populates its tiles/buildings as children in Start().
            // If childCount is still 0, that grid likely has not finished building yet.
            if (entry.grid.transform.childCount == 0)
                return false;
        }

        return foundAtLeastOneValidGrid;
    }

    [ContextMenu("Rebuild Warehouses")]
    public void RebuildWarehouses()
    {
        if (warehousePrefab == null)
        {
            Debug.LogWarning("[WarehouseSceneOverlay] Warehouse prefab is not assigned.");
            return;
        }

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            Debug.LogWarning("[WarehouseSceneOverlay] Database is not ready yet.");
            return;
        }

        ClearSpawnedWarehousesOnly();

        List<Warehouse> warehouses = WarehouseService.GetAllOwned();
        for (int i = 0; i < warehouses.Count; i++)
        {
            Warehouse warehouse = warehouses[i];
            if (warehouse == null)
                continue;

            ApplyWarehouseToZoneTile(warehouse);
        }
    }

    private void ClearSpawnedWarehousesOnly()
    {
        for (int i = spawnedWarehouses.Count - 1; i >= 0; i--)
        {
            if (spawnedWarehouses[i] != null)
                Destroy(spawnedWarehouses[i]);
        }

        spawnedWarehouses.Clear();
    }

    private void ApplyWarehouseToZoneTile(Warehouse warehouse)
    {
        if (warehouse == null)
            return;

        // Starter warehouse already exists through your other setup.
        if (warehouse.isStarterWarehouse == 1)
            return;

        gridify zoneGrid = GetGridForZone(warehouse.zoneName);
        if (zoneGrid == null)
        {
            Debug.LogWarning("[WarehouseSceneOverlay] No grid found for zone: " + warehouse.zoneName);
            return;
        }

        Vector3 tileWorldPos = WarehouseService.TileToWorld(zoneGrid, warehouse.tileX, warehouse.tileZ);

        RemoveGeneratedObjectsAtTile(zoneGrid, tileWorldPos);
        SpawnGroundAtTile(zoneGrid, tileWorldPos);

        GameObject warehouseGo = Instantiate(
            warehousePrefab,
            tileWorldPos,
            Quaternion.Euler(-90f, 0f, 0f),
            zoneGrid.transform
        );

        warehouseGo.transform.localScale = zoneGrid.houseScale;

        WarehouseInstanceMarker marker = warehouseGo.GetComponent<WarehouseInstanceMarker>();
        if (marker == null)
            marker = warehouseGo.AddComponent<WarehouseInstanceMarker>();

        marker.warehouseId = warehouse.id;
        marker.zoneName = warehouse.zoneName;
        marker.tileX = warehouse.tileX;
        marker.tileZ = warehouse.tileZ;

        AssignWarehouseIdentity(warehouseGo, warehouse);

        spawnedWarehouses.Add(warehouseGo);
    }

    private void AssignWarehouseIdentity(GameObject warehouseGo, Warehouse warehouse)
    {
        if (warehouseGo == null || warehouse == null)
            return;

        WarehouseIdentity identity = warehouseGo.GetComponent<WarehouseIdentity>();

        if (identity == null)
            identity = warehouseGo.GetComponentInChildren<WarehouseIdentity>(true);

        if (identity == null)
        {
            Debug.LogWarning(
                $"[WarehouseSceneOverlay] Spawned warehouse prefab '{warehouseGo.name}' has no WarehouseIdentity component."
            );
            return;
        }

        identity.SetIdentity(
            warehouse.id,
            warehouse.zoneName,
            warehouse.tileX,
            warehouse.tileZ
        );

        Debug.Log(
            $"[WarehouseSceneOverlay] Assigned WarehouseIdentity for warehouse ID {warehouse.id} at {warehouse.zoneName} {warehouse.tileX}, {warehouse.tileZ}"
        );
    }

    private void SpawnGroundAtTile(gridify zoneGrid, Vector3 tileWorldPos)
    {
        if (zoneGrid == null || zoneGrid.groundSquare == null)
            return;

        GameObject ground = Instantiate(
            zoneGrid.groundSquare,
            tileWorldPos,
            Quaternion.Euler(-90f, 0f, 0f),
            zoneGrid.transform
        );

        ground.transform.localScale = zoneGrid.houseScale;
    }

    private void RemoveGeneratedObjectsAtTile(gridify zoneGrid, Vector3 tileWorldPos)
    {
        if (zoneGrid == null)
            return;

        List<GameObject> toDestroy = new List<GameObject>();

        Transform parent = zoneGrid.transform;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (Vector3.Distance(child.position, tileWorldPos) > positionTolerance)
                continue;

            if (child.GetComponent<WarehouseInstanceMarker>() != null)
                continue;

            toDestroy.Add(child.gameObject);
        }

        for (int i = 0; i < toDestroy.Count; i++)
            Destroy(toDestroy[i]);
    }

    private gridify GetGridForZone(string zoneName)
    {
        for (int i = 0; i < zoneGrids.Count; i++)
        {
            WarehouseZoneGrid entry = zoneGrids[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.zoneName))
                continue;

            if (string.Equals(entry.zoneName, zoneName, StringComparison.OrdinalIgnoreCase))
                return entry.grid;
        }

        return null;
    }
}