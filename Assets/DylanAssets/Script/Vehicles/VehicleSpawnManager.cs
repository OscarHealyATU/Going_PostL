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
        if (db == null)
            return false;

        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null)
            return false;

        bool shouldSpawnInThisScene =
            string.Equals(vehicle.spawnScene, SceneName, StringComparison.Ordinal) ||
            (vehicle.hasSavedLocation != 0 && string.Equals(vehicle.savedScene, SceneName, StringComparison.Ordinal));

        if (!shouldSpawnInThisScene)
            return false;

        if (spawnedVehicleIds.Contains(vehicle.id))
            return true;

        if (FindExistingVehicle(vehicle.id) != null)
        {
            spawnedVehicleIds.Add(vehicle.id);
            return true;
        }

        var type = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (type == null)
            return false;

        if (!catalog.TryGetPrefab(type.name, out GameObject prefab) || prefab == null)
            return false;

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        bool useSavedLocation =
            vehicle.hasSavedLocation != 0 &&
            string.Equals(vehicle.savedScene, SceneName, StringComparison.Ordinal);

        if (useSavedLocation)
        {
            spawnPosition = new Vector3(vehicle.savedX, vehicle.savedY, vehicle.savedZ);
            spawnRotation = Quaternion.Euler(0f, vehicle.savedYaw, 0f);
        }
        else
        {
            if (!TryGetSpawnPointPose(vehicle.spawnBay, out spawnPosition, out spawnRotation))
                return false;

            if (IsSpawnPointOccupiedByAnotherVehicle(vehicle.spawnBay, vehicle.id))
                return false;
        }

        GameObject spawned = Instantiate(prefab, spawnPosition, spawnRotation);
        spawned.name = $"Vehicle_{vehicle.id}_{type.name}";

        ApplyVehicleLinkData(spawned, vehicle);

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
        if (db == null)
            return;

        var vehiclesForScene = db.Table<Vehicle>()
            .Where(v =>
                v.spawnScene == SceneName ||
                (v.hasSavedLocation != 0 && v.savedScene == SceneName))
            .OrderBy(v => v.spawnBay)
            .ThenBy(v => v.id)
            .ToList();

        for (int i = 0; i < vehiclesForScene.Count; i++)
        {
            TrySpawnVehicle(vehiclesForScene[i].id);
        }
    }

    public bool TryRecoverVehicleToAssignedBay(int vehicleId, out string message)
    {
        message = "Vehicle recovery failed.";

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            message = "Database not available.";
            return false;
        }

        var db = DbBoot.Instance.Db;
        var vehicle = db.Find<Vehicle>(vehicleId);

        if (vehicle == null)
        {
            message = "Vehicle not found.";
            return false;
        }

        if (!string.Equals(vehicle.spawnScene, SceneName, StringComparison.Ordinal))
        {
            message = $"This vehicle belongs to scene '{vehicle.spawnScene}', not '{SceneName}'.";
            return false;
        }

        if (!TryGetSpawnPointPose(vehicle.spawnBay, out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            message = $"Assigned spawn point {vehicle.spawnBay + 1} is invalid or missing.";
            return false;
        }

        var existingLink = FindExistingVehicle(vehicle.id);

        db.RunInTransaction(() =>
        {
            vehicle.hasSavedLocation = 0;
            vehicle.savedScene = null;
            vehicle.savedX = 0f;
            vehicle.savedY = 0f;
            vehicle.savedZ = 0f;
            vehicle.savedYaw = 0f;
            vehicle.spawnPending = 0;
            db.Update(vehicle);
        });

        if (existingLink != null)
        {
            Transform root = existingLink.transform.root;
            Rigidbody rb = root.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = spawnPosition;
                rb.rotation = spawnRotation;
            }
            else
            {
                root.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            message = $"Vehicle returned to parking space {vehicle.spawnBay + 1}.";
            return true;
        }

        bool spawned = TrySpawnVehicle(vehicle.id);
        if (!spawned)
        {
            message = $"Could not respawn vehicle in parking space {vehicle.spawnBay + 1}.";
            return false;
        }

        message = $"Vehicle returned to parking space {vehicle.spawnBay + 1}.";
        return true;
    }

    public bool TryGetSpawnPointPose(int spawnPointIndex, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Length)
            return false;

        var point = spawnPoints[spawnPointIndex];
        if (point == null)
            return false;

        position = point.transform.position;
        rotation = point.transform.rotation;
        return true;
    }

    private void ApplyVehicleLinkData(GameObject spawned, Vehicle vehicle)
    {
        if (spawned == null || vehicle == null)
            return;

        var links = spawned.GetComponentsInChildren<VehicleLink>(true);

        if (links == null || links.Length == 0)
        {
            var rootLink = spawned.AddComponent<VehicleLink>();
            rootLink.vehicleId = vehicle.id;

            Debug.Log($"[VehicleSpawnManager] Added VehicleLink to '{spawned.name}' with vehicleId={vehicle.id}");
            return;
        }

        for (int i = 0; i < links.Length; i++)
        {
            if (links[i] == null)
                continue;

            links[i].vehicleId = vehicle.id;
        }

        Debug.Log($"[VehicleSpawnManager] Applied vehicleId={vehicle.id} to {links.Length} VehicleLink component(s) on '{spawned.name}'");
    }

    private VehicleLink FindExistingVehicle(int vehicleId)
    {
        var allLinks = FindObjectsByType<VehicleLink>(FindObjectsSortMode.None);
        for (int i = 0; i < allLinks.Length; i++)
        {
            if (allLinks[i] != null && allLinks[i].vehicleId == vehicleId)
                return allLinks[i];
        }

        return null;
    }

    private bool IsSpawnPointOccupiedByAnotherVehicle(int spawnPointIndex, int vehicleId)
    {
        var allLinks = FindObjectsByType<VehicleLink>(FindObjectsSortMode.None);

        for (int i = 0; i < allLinks.Length; i++)
        {
            var link = allLinks[i];
            if (link == null)
                continue;

            if (link.vehicleId == vehicleId)
                continue;

            if (IsVehicleAssignedToSpawnPoint(link.vehicleId, spawnPointIndex))
                return true;
        }

        if (spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Length)
        {
            var point = spawnPoints[spawnPointIndex];
            if (point != null && point.IsOccupied())
                return true;
        }

        return false;
    }

    private bool IsVehicleAssignedToSpawnPoint(int vehicleId, int spawnPointIndex)
    {
        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        var otherVehicle = DbBoot.Instance.Db.Find<Vehicle>(vehicleId);
        if (otherVehicle == null)
            return false;

        if (!string.Equals(otherVehicle.spawnScene, SceneName, StringComparison.Ordinal))
            return false;

        if (otherVehicle.hasSavedLocation != 0 && string.Equals(otherVehicle.savedScene, SceneName, StringComparison.Ordinal))
            return false;

        return otherVehicle.spawnBay == spawnPointIndex;
    }
}