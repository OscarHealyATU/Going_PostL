using System.Linq;
using UnityEngine;

public class VehicleCatalogValidator : MonoBehaviour
{
    public VehicleCatalog catalog;

    void Start()
    {
        if (catalog == null)
        {
            Debug.LogError("VehicleCatalogValidator: catalog not assigned.");
            return;
        }

        var dbNames = VehicleTypeStore.All.Select(v => v.name).ToHashSet();
        foreach (var entry in catalog.entries)
        {
            if (entry.prefab == null)
                Debug.LogWarning($"Catalog entry '{entry.vehicleTypeName}' has no prefab assigned.");

            if (!dbNames.Contains(entry.vehicleTypeName))
                Debug.LogWarning($"Catalog entry '{entry.vehicleTypeName}' does NOT match any VehicleType.name in the DB.");
        }

        Debug.Log("VehicleCatalog validation complete.");
    }
}
