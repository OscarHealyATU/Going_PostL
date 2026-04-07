using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VehicleTypeStore
{
    public static List<VehicleType> All { get; private set; } = new List<VehicleType>();

    public static void LoadOrSeedDefaults(SQLite.SQLiteConnection db)
    {
        db.CreateTable<VehicleType>();
        All = db.Table<VehicleType>().OrderBy(v => v.baseCost).ToList();

        Debug.Log("[VehicleTypeStore] Loaded vehicle types = " + All.Count);
    }

    public static void Load()
    {
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("[VehicleTypeStore] DbBoot.Instance is null - cannot Load()");
            All = new List<VehicleType>();
            return;
        }

        LoadOrSeedDefaults(DbBoot.Instance.Db);
    }
}