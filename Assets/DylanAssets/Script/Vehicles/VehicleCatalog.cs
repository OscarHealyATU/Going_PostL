using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DeliveryGame/Vehicle Catalog")]
public class VehicleCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string vehicleTypeName;
        public GameObject prefab;
    }

    public List<Entry> entries = new List<Entry>();

    public bool TryGetPrefab(string vehicleTypeName, out GameObject prefab)
    {
        foreach (var entry in entries)
        {
            if (entry.prefab == null) continue;

            if (string.Equals(entry.vehicleTypeName?.Trim(), vehicleTypeName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                prefab = entry.prefab;
                return true;
            }
        }

        prefab = null;
        return false;
    }
}