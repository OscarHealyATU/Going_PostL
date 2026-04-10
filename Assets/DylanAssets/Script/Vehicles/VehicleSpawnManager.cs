using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VehicleSpawnManager : MonoBehaviour
{
    public static VehicleSpawnManager Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private string sceneNameOverride = "";

    [Header("Spawn Points")]
    [SerializeField] private VehicleSpawnPoint[] spawnPoints = new VehicleSpawnPoint[4];

    [Header("Vehicle Catalog")]
    [SerializeField] private VehicleCatalog catalog;

    private readonly HashSet<int> spawnedVehicleIds = new HashSet<int>();
    private bool hasSpawnedForCurrentScene = false;

    private string SceneName =>
        string.IsNullOrWhiteSpace(sceneNameOverride)
            ? SceneManager.GetActiveScene().name
            : sceneNameOverride.Trim();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TrySpawnForActiveScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.Equals(scene.name, SceneName, StringComparison.Ordinal))
            return;

        hasSpawnedForCurrentScene = false;
        spawnedVehicleIds.Clear();

        TrySpawnForActiveScene();
    }

    private void TrySpawnForActiveScene()
    {
        if (hasSpawnedForCurrentScene)
            return;

        SpawnVehiclesForThisScene();
        hasSpawnedForCurrentScene = true;
    }

    public bool TrySpawnVehicle(int vehicleId)
    {
        if (DbBoot.Instance == null || catalog == null)
            return false;

        var db = DbBoot.Instance.Db;
        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null)
            return false;

        if (!string.Equals(vehicle.spawnScene, SceneName, StringComparison.Ordinal))
            return false;

        if (vehicle.spawnBay < 0 || vehicle.spawnBay >= spawnPoints.Length)
            return false;

        if (spawnedVehicleIds.Contains(vehicle.id))
            return true;

        if (FindExistingVehicle(vehicle.id) != null)
        {
            spawnedVehicleIds.Add(vehicle.id);
            return true;
        }

        var point = spawnPoints[vehicle.spawnBay];
        if (point == null)
            return false;

        if (IsSpawnPointOccupiedByAnotherVehicle(vehicle.spawnBay, vehicle.id))
            return false;

        var type = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (type == null)
            return false;

        if (!catalog.TryGetPrefab(type.name, out GameObject prefab) || prefab == null)
            return false;

        GameObject spawned = Instantiate(prefab, point.transform.position, point.transform.rotation);
        spawned.name = $"Vehicle_{vehicle.id}_{type.name}";

        VehicleLink link = spawned.GetComponent<VehicleLink>();
        if (link == null)
            link = spawned.AddComponent<VehicleLink>();

        link.vehicleId = vehicle.id;
        link.spawnPointIndex = vehicle.spawnBay;
        link.spawnScene = vehicle.spawnScene;

        spawnedVehicleIds.Add(vehicle.id);

        if (vehicle.spawnPending != 0)
        {
            vehicle.spawnPending = 0;
            db.Update(vehicle);
        }

        return true;
    }

    public void SpawnVehiclesForThisScene()
    {
        if (DbBoot.Instance == null || catalog == null)
            return;

        var db = DbBoot.Instance.Db;

        var vehiclesForScene = db.Table<Vehicle>()
            .Where(v => v.spawnScene == SceneName)
            .Where(v => v.spawnBay >= 0)
            .OrderBy(v => v.spawnBay)
            .ThenBy(v => v.id)
            .ToList();

        for (int i = 0; i < vehiclesForScene.Count; i++)
        {
            var vehicle = vehiclesForScene[i];

            // Spawn every vehicle assigned to this scene,
            // not only newly-purchased pending ones.
            TrySpawnVehicle(vehicle.id);
        }
    }

    private VehicleLink FindExistingVehicle(int vehicleId)
    {
        var allLinks = FindObjectsOfType<VehicleLink>(true);
        for (int i = 0; i < allLinks.Length; i++)
        {
            if (allLinks[i] != null && allLinks[i].vehicleId == vehicleId)
                return allLinks[i];
        }

        return null;
    }

    private bool IsSpawnPointOccupiedByAnotherVehicle(int spawnPointIndex, int vehicleId)
    {
        var allLinks = FindObjectsOfType<VehicleLink>(true);

        for (int i = 0; i < allLinks.Length; i++)
        {
            var link = allLinks[i];
            if (link == null)
                continue;

            if (link.vehicleId == vehicleId)
                continue;

            if (!string.Equals(link.spawnScene, SceneName, StringComparison.Ordinal))
                continue;

            if (link.spawnPointIndex == spawnPointIndex)
                return true;
        }

        // Optional fallback if your VehicleSpawnPoint has its own occupancy logic.
        if (spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Length)
        {
            var point = spawnPoints[spawnPointIndex];
            if (point != null)
            {
                var pointOccupied = point.IsOccupied();
                if (pointOccupied)
                    return true;
            }
        }

        return false;
    }
}