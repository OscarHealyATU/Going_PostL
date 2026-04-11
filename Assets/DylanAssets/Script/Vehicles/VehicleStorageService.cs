using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VehicleStorageService
{
    public struct StoreResult
    {
        public bool success;
        public string message;
        public StoredDelivery storedDelivery;
    }

    public struct UnstoreResult
    {
        public bool success;
        public string message;
        public DeliveryJob restoredDeliveryJob;
    }

    public static int GetCapacity(int vehicleId)
    {
        if (DbBoot.Instance == null)
            return 0;

        var db = DbBoot.Instance.Db;
        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null)
            return 0;

        var type = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (type == null)
            return 0;

        return Mathf.Max(0, type.storageCapacity);
    }

    public static List<StoredDelivery> GetStoredDeliveries(int vehicleId)
    {
        if (DbBoot.Instance == null)
            return new List<StoredDelivery>();

        var db = DbBoot.Instance.Db;

        return db.Table<StoredDelivery>()
            .Where(x => x.vehicleId == vehicleId)
            .OrderBy(x => x.slotIndex)
            .ToList();
    }

    public static StoredDelivery GetStoredDeliveryInSlot(int vehicleId, int slotIndex)
    {
        if (DbBoot.Instance == null)
            return null;

        var db = DbBoot.Instance.Db;

        return db.Table<StoredDelivery>()
            .FirstOrDefault(x => x.vehicleId == vehicleId && x.slotIndex == slotIndex);
    }

    public static StoredDelivery GetStoredDeliveryById(int storedDeliveryId)
    {
        if (DbBoot.Instance == null)
            return null;

        return DbBoot.Instance.Db.Find<StoredDelivery>(storedDeliveryId);
    }

    public static bool IsSlotOccupied(int vehicleId, int slotIndex)
    {
        return GetStoredDeliveryInSlot(vehicleId, slotIndex) != null;
    }

    public static int GetUsedSlotCount(int vehicleId)
    {
        if (DbBoot.Instance == null)
            return 0;

        var db = DbBoot.Instance.Db;

        return db.Table<StoredDelivery>()
            .Count(x => x.vehicleId == vehicleId);
    }

    public static int GetFreeSlotCount(int vehicleId)
    {
        int capacity = GetCapacity(vehicleId);
        int used = GetUsedSlotCount(vehicleId);
        return Mathf.Max(0, capacity - used);
    }

    public static int GetFirstEmptySlot(int vehicleId)
    {
        if (DbBoot.Instance == null)
            return -1;

        var db = DbBoot.Instance.Db;

        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null)
            return -1;

        var vehicleType = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (vehicleType == null)
            return -1;

        int capacity = Mathf.Max(0, vehicleType.storageCapacity);

        var usedSlots = db.Table<StoredDelivery>()
            .Where(x => x.vehicleId == vehicleId)
            .Select(x => x.slotIndex)
            .ToList();

        for (int i = 0; i < capacity; i++)
        {
            if (!usedSlots.Contains(i))
                return i;
        }

        return -1;
    }

    public static StoreResult StoreDeliveryInVehicle(int vehicleId, int deliveryJobId, int slotIndex)
    {
        var result = new StoreResult
        {
            success = false,
            message = "Failed to store delivery.",
            storedDelivery = null
        };

        if (DbBoot.Instance == null)
        {
            result.message = "Database not available.";
            return result;
        }

        var db = DbBoot.Instance.Db;

        var vehicle = db.Find<Vehicle>(vehicleId);
        if (vehicle == null)
        {
            result.message = "Vehicle not found.";
            return result;
        }

        var vehicleType = db.Find<VehicleType>(vehicle.vehicleTypeId);
        if (vehicleType == null)
        {
            result.message = "Vehicle type not found.";
            return result;
        }

        if (slotIndex < 0 || slotIndex >= vehicleType.storageCapacity)
        {
            result.message = "Invalid storage slot.";
            return result;
        }

        bool slotOccupied = db.Table<StoredDelivery>()
            .Any(x => x.vehicleId == vehicleId && x.slotIndex == slotIndex);

        if (slotOccupied)
        {
            result.message = "That vehicle slot is already occupied.";
            return result;
        }

        var job = db.Find<DeliveryJob>(deliveryJobId);
        if (job == null)
        {
            result.message = "Delivery job not found.";
            return result;
        }

        StoredDelivery created = null;

        try
        {
            db.RunInTransaction(() =>
            {
                created = new StoredDelivery
                {
                    vehicleId = vehicleId,
                    originalDeliveryJobId = job.id,
                    itemId = job.itemId,
                    itemName = job.itemName,
                    targetX = job.targetX,
                    targetY = job.targetY,
                    targetZ = job.targetZ,
                    zoneId = job.zoneId,
                    slotIndex = slotIndex,
                    storedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Insert(created);
                db.Delete(job);
            });

            result.success = true;
            result.message = "Delivery stored in vehicle.";
            result.storedDelivery = created;
            return result;
        }
        catch (Exception ex)
        {
            result.message = ex.Message;
            return result;
        }
    }

    public static UnstoreResult RemoveStoredDeliveryFromVehicle(int storedDeliveryId)
    {
        var result = new UnstoreResult
        {
            success = false,
            message = "Failed to remove stored delivery.",
            restoredDeliveryJob = null
        };

        if (DbBoot.Instance == null)
        {
            result.message = "Database not available.";
            return result;
        }

        var db = DbBoot.Instance.Db;

        var stored = db.Find<StoredDelivery>(storedDeliveryId);
        if (stored == null)
        {
            result.message = "Stored delivery not found.";
            return result;
        }

        DeliveryJob recreated = null;

        try
        {
            db.RunInTransaction(() =>
            {
                recreated = new DeliveryJob
                {
                    itemId = stored.itemId,
                    itemName = stored.itemName,
                    status = 0,
                    targetX = stored.targetX,
                    targetY = stored.targetY,
                    targetZ = stored.targetZ,
                    zoneId = stored.zoneId,
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.Insert(recreated);
                db.Delete(stored);
            });

            result.success = true;
            result.message = "Delivery moved back to active jobs.";
            result.restoredDeliveryJob = recreated;
            return result;
        }
        catch (Exception ex)
        {
            result.message = ex.Message;
            return result;
        }
    }

    public static UnstoreResult RemoveStoredDeliveryFromVehicleSlot(int vehicleId, int slotIndex)
    {
        if (DbBoot.Instance == null)
        {
            return new UnstoreResult
            {
                success = false,
                message = "Database not available.",
                restoredDeliveryJob = null
            };
        }

        var db = DbBoot.Instance.Db;

        var stored = db.Table<StoredDelivery>()
            .FirstOrDefault(x => x.vehicleId == vehicleId && x.slotIndex == slotIndex);

        if (stored == null)
        {
            return new UnstoreResult
            {
                success = false,
                message = "No stored delivery found in that slot.",
                restoredDeliveryJob = null
            };
        }

        return RemoveStoredDeliveryFromVehicle(stored.id);
    }
}