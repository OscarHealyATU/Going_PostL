using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DeliveryService
{
    public static List<DeliveryJob> GetAll()
    {
        return DbBoot.Instance.Db.Table<DeliveryJob>()
            .OrderBy(j => j.id)
            .ToList();
    }

    public static List<DeliveryJob> GetPending()
    {
        return DbBoot.Instance.Db.Table<DeliveryJob>()
            .Where(j => j.status == 0 || j.status == 1)
            .OrderBy(j => j.id)
            .ToList();
    }

    public static DeliveryJob GetCurrent()
    {
        return GetPending().FirstOrDefault();
    }

    public static DeliveryJob Create(string itemId, string itemName, Vector3 targetPosition)
    {
        return Create(itemId, itemName, targetPosition, 1);
    }

    public static DeliveryJob Create(string itemId, string itemName, Vector3 targetPosition, int zoneId)
    {
        Debug.Log($"DeliveryService.Create called: itemId={itemId}, itemName={itemName}, target={targetPosition}, zoneId={zoneId}");

        var db = DbBoot.Instance.Db;

        var job = new DeliveryJob
        {
            itemId = itemId,
            itemName = itemName,
            status = 0,
            targetX = targetPosition.x,
            targetY = targetPosition.y,
            targetZ = targetPosition.z,
            zoneId = zoneId,
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        db.Insert(job);

        Debug.Log("DeliveryService.Create inserted row with id=" + job.id);
        return job;
    }

    public static void SetActive(int id)
    {
        var db = DbBoot.Instance.Db;
        var job = db.Table<DeliveryJob>().FirstOrDefault(j => j.id == id);
        if (job == null) return;

        job.status = 1;
        db.Update(job);
    }

    public static void Complete(int id, double moneyEarned, int experienceEarned)
    {
        var db = DbBoot.Instance.Db;

        var job = db.Table<DeliveryJob>().FirstOrDefault(j => j.id == id);
        if (job == null)
        {
            Debug.LogWarning($"DeliveryService.Complete ignored: job {id} was already completed or does not exist.");
            return;
        }

        db.Delete(job);

        Debug.Log($"DeliveryService.Complete succeeded for job {id}.");
    }

    public static Vector3 GetTargetPosition(DeliveryJob job)
    {
        return new Vector3(job.targetX, job.targetY, job.targetZ);
    }

    public static int GetZoneId(DeliveryJob job)
    {
        if (job == null)
            return 1;

        return job.zoneId <= 0 ? 1 : job.zoneId;
    }
}