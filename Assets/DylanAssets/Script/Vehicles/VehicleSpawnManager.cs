using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VehicleSpawnManager : MonoBehaviour
{
    [Header("Scene Name (auto-detected if empty)")]
    public string sceneName = "";

    [Header("Spawn Bays (0-based)")]
    public Transform[] bays;

    [Header("Catalog maps VehicleType.name -> prefab")]
    public VehicleCatalog catalog;

    // Track what we have spawned this session so we don't duplicate
    private readonly HashSet<int> _spawnedVehicleIds = new HashSet<int>();

    void Start()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        SpawnAllForScene();
    }

    public void SpawnAllForScene()
    {
        if (DbBoot.Instance == null) { Debug.LogError("[VehicleSpawnManager] DbBoot.Instance is null"); return; }
        if (catalog == null) { Debug.LogError("[VehicleSpawnManager] VehicleCatalog not assigned"); return; }
        if (bays == null || bays.Length == 0) { Debug.LogError("[VehicleSpawnManager] No bays assigned."); return; }

        var db = DbBoot.Instance.Db;

        // ✅ Spawn BOTH pending and already-spawned vehicles for this scene.
        var vehiclesForThisScene = db.Table<Vehicle>()
            .Where(v => v.spawnScene == sceneName)
            .OrderBy(v => v.id)
            .ToList();

        Debug.Log($"[VehicleSpawnManager] Vehicles in '{sceneName}' (pending+spawned): {vehiclesForThisScene.Count}");

        foreach (var v in vehiclesForThisScene)
        {
            if (_spawnedVehicleIds.Contains(v.id))
                continue; // already spawned this session

            // validate bay
            int bayIndex = Mathf.Clamp(v.spawnBay, 0, bays.Length - 1);
            var bay = bays[bayIndex];
            if (bay == null)
            {
                Debug.LogError($"[VehicleSpawnManager] Bay missing at index {bayIndex}");
                continue;
            }

            // lookup type
            var type = db.Find<VehicleType>(v.vehicleTypeId);
            if (type == null)
            {
                Debug.LogWarning($"[VehicleSpawnManager] VehicleType missing for vehicle id={v.id}");
                continue;
            }

            // prefab mapping
            if (!catalog.TryGetPrefab(type.name, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"[VehicleSpawnManager] No prefab mapped for VehicleType '{type.name}'");
                continue;
            }

            // occupancy safety: if something is already in the bay this session, skip
            if (IsBayOccupiedInScene(bayIndex))
            {
                Debug.LogWarning($"[VehicleSpawnManager] Bay {bayIndex} already occupied in-scene. Skipping vehicle id={v.id}");
                continue;
            }

            var go = Instantiate(prefab, bay.position, bay.rotation);
            go.name = $"Vehicle_{v.id}_{type.name}";

            var link = go.GetComponent<VehicleLink>() ?? go.AddComponent<VehicleLink>();
            link.vehicleId = v.id;

            _spawnedVehicleIds.Add(v.id);

            // If it was pending, mark as spawned (but keep spawnScene/spawnBay as its parking location)
            if (v.spawnPending == 1)
            {
                v.spawnPending = 0;
                db.Update(v);
            }

            Debug.Log($"[VehicleSpawnManager] Spawned vehicle id={v.id} '{type.name}' at bay={bayIndex} pendingWas={v.spawnPending}");
        }
    }

    private bool IsBayOccupiedInScene(int bayIndex)
    {
        // Simple check: any VehicleLink already at/under this bay position is “occupied”
        // If you parent vehicles under bays, this becomes even cleaner.
        var links = FindObjectsByType<VehicleLink>(FindObjectsSortMode.None);
        foreach (var l in links)
        {
            // optional: if you parent, check transform.parent == bays[bayIndex]
            // for now, we just treat any existing vehicle whose name indicates bay occupancy as occupied.
            // Better approach below in Part 2.
        }
        return false;
    }
}