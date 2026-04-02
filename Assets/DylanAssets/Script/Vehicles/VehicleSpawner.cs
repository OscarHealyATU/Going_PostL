using System;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [Header("Assign your VehicleCatalog asset here")]
    public VehicleCatalog catalog;

    [Header("Exactly 4 bay transforms")]
    public Transform[] bays = new Transform[4];

    public GameObject SpawnVehicle(int vehicleId, int bayIndex0Based)
    {
        if (catalog == null) throw new Exception("VehicleSpawner: catalog is not assigned.");
        if (bays == null || bays.Length != 4) throw new Exception("VehicleSpawner: bays must be length 4.");
        if (bayIndex0Based < 0 || bayIndex0Based > 3) throw new Exception("VehicleSpawner: bayIndex must be 0..3.");

        var db = DbBoot.Instance.Db;

        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null) throw new Exception($"VehicleSpawner: Vehicle id={vehicleId} not found.");

        var type = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (type == null) throw new Exception($"VehicleSpawner: VehicleType id={vehicle.vehicleTypeId} not found.");

        if (!catalog.TryGetPrefab(type.name, out var prefab) || prefab == null)
            throw new Exception($"VehicleSpawner: No prefab mapped for VehicleType.name='{type.name}'. Add it to VehicleCatalog.");

        var bay = bays[bayIndex0Based];
        var go = Instantiate(prefab, bay.position, bay.rotation);

        // Attach/link DB id so you can track this instance later
        var link = go.GetComponent<VehicleLink>();
        if (link == null) link = go.AddComponent<VehicleLink>();
        link.vehicleId = vehicle.id;

        return go;
    }
}
