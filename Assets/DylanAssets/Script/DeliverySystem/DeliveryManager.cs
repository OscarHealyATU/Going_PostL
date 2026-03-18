using System.Linq;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("Player")]
    public Transform player;

    [Header("Delivery")]
    public float completeRadius = 4f;

    [Header("Optional world marker")]
    public Transform activeMarker;

    private DeliveryJob currentJob;

    public DeliveryJob CurrentJob => currentJob;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshCurrentJob();
    }

    private void Update()
    {
        if (currentJob == null || player == null) return;

        Vector3 target = DeliveryService.GetTargetPosition(currentJob);

        if (activeMarker != null)
            activeMarker.position = target;

        float dist = Vector3.Distance(player.position, target);
        if (dist <= completeRadius)
        {
            CompleteCurrentDelivery();
        }
    }

    public void AddDelivery(string itemId, string itemName)
    {
        if (DeliveryGridProvider.Instance == null)
        {
            Debug.LogWarning("DeliveryManager: DeliveryGridProvider missing.");
            return;
        }

        Vector3 point = DeliveryGridProvider.Instance.GetRandomPoint();
        var job = DeliveryService.Create(itemId, itemName, point);

        Debug.Log($"Created delivery job #{job.id} for {itemName} at {point}");

        if (currentJob == null)
            RefreshCurrentJob();
    }

    public void RefreshCurrentJob()
    {
        currentJob = DeliveryService.GetCurrent();

        if (currentJob != null && currentJob.status == 0)
        {
            DeliveryService.SetActive(currentJob.id);
            currentJob.status = 1;
        }
    }

    private void CompleteCurrentDelivery()
    {
        if (currentJob == null) return;

        Debug.Log($"Completed delivery #{currentJob.id} ({currentJob.itemName})");

        if (InventoryManager.Instance != null)
        {
            bool removed = InventoryManager.Instance.RemoveFirstMatchingItem(currentJob.itemId);

            if (!removed)
            {
                Debug.LogWarning($"Delivery complete, but no matching inventory item was found for key: {currentJob.itemId}");
            }
        }

        DeliveryService.Complete(currentJob.id);

        currentJob = null;
        RefreshCurrentJob();
    }

    public Vector3? GetCurrentTarget()
    {
        if (currentJob == null) return null;
        return DeliveryService.GetTargetPosition(currentJob);
    }
}