using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DeliveryGame/Vehicle Catalog")]
public class VehicleCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Must EXACTLY match VehicleType.name in the database (case-insensitive).")]
        public string vehicleTypeName;

        [Tooltip("Prefab to spawn for this vehicle type.")]
        public GameObject prefab;
    }

    public List<Entry> entries = new List<Entry>();

    public bool TryGetPrefab(string vehicleTypeName, out GameObject prefab)
    {
        foreach (var e in entries)
        {
            if (e.prefab == null) continue;

            if (string.Equals(e.vehicleTypeName?.Trim(), vehicleTypeName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                prefab = e.prefab;
                return true;
            }
        }

        prefab = null;
        return false;
    }
}
