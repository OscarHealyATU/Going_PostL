using System;
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

    private string SceneName =>
        string.IsNullOrWhiteSpace(sceneNameOverride)
            ? SceneManager.GetActiveScene().name
            : sceneNameOverride.Trim();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnVehiclesForThisScene();
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

        var point = spawnPoints[vehicle.spawnBay];
        if (point == null || point.IsOccupied())
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

        vehicle.spawnPending = 0;
        db.Update(vehicle);

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

            if (vehicle.spawnPending != 1)
                continue;

            TrySpawnVehicle(vehicle.id);
        }
    }
}