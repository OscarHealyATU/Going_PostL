using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VehicleTypeStore
{
    public static List<VehicleType> All { get; private set; } = new List<VehicleType>();

    public static void LoadOrSeedDefaults(SQLite.SQLiteConnection db)
    {
        db.CreateTable<VehicleType>();

        int before = db.Table<VehicleType>().Count();
        Debug.Log("[VehicleTypeStore] rows BEFORE seed = " + before);

        if (before == 0)
        {
            db.Insert(new VehicleType { name="Zone 1", baseCost=5000 });
            db.Insert(new VehicleType { name="Zone 2", baseCost=9000 });
            db.Insert(new VehicleType { name="Zone 3", baseCost=14000 });
            db.Insert(new VehicleType { name="Zone 4", baseCost=20000 });

            Debug.Log("[VehicleTypeStore] Seeded defaults.");
        }

        All = db.Table<VehicleType>().ToList();
        Debug.Log("[VehicleTypeStore] Loaded into All = " + All.Count);
    }

    public static void Load()
    {
        // Route through DbBoot so it ALWAYS uses the correct DB.
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("[VehicleTypeStore] DbBoot.Instance is null - cannot Load()");
            All = new List<VehicleType>();
            return;
        }

        LoadOrSeedDefaults(DbBoot.Instance.Db);
    }
}