using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Delivery")]
    public float completeRadius = 10f;
    public string mainSceneName = "Main";

    [Header("Optional world marker")]
    public Transform activeMarker;

    [Header("Delivery Point Prefab")]
    public DeliveryPointInteractable deliveryPointPrefab;

    [Header("Delivery Point Placement")]
    [Tooltip("How far from the center of the grid space to place the delivery point.")]
    [SerializeField] private float borderOffsetDistance = 1.25f;

    [Tooltip("Extra vertical offset for the spawned delivery point.")]
    [SerializeField] private float deliveryPointHeightOffset = 0f;

    [Header("Rewards")]
    [SerializeField] private double basePay = 100.0;
    [SerializeField] private int baseDeliveryExperience = PlayerService.ExpPerDelivery;

    private DeliveryJob currentJob;
    private DeliveryPointInteractable activeDeliveryPoint;

    public DeliveryJob CurrentJob => currentJob;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TryFindPlayer();
        RefreshCurrentJob();
        RefreshDeliveryPoint();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        TryFindPlayer();
        RefreshCurrentJob();
        RefreshDeliveryPoint();
    }

    private void Update()
    {
        TryFindPlayer();

        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            if (activeMarker != null)
                activeMarker.gameObject.SetActive(false);

            if (activeDeliveryPoint != null)
                activeDeliveryPoint.gameObject.SetActive(false);

            return;
        }

        if (currentJob == null || player == null)
        {
            if (activeMarker != null)
                activeMarker.gameObject.SetActive(false);

            if (activeDeliveryPoint != null)
                activeDeliveryPoint.gameObject.SetActive(false);

            return;
        }

        Vector3 target = DeliveryService.GetTargetPosition(currentJob);

        if (activeMarker != null)
        {
            activeMarker.gameObject.SetActive(true);
            activeMarker.position = target;
        }

        RefreshDeliveryPoint();
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            player = go.transform;
    }

    public void AddDelivery(string itemId, string itemName)
    {
        Debug.Log($"AddDelivery called with itemId={itemId}, itemName={itemName}");

        if (DeliveryGridProvider.Instance == null)
        {
            Debug.LogWarning("DeliveryManager: DeliveryGridProvider missing.");
            return;
        }

        DeliveryZone selectedZone = GetRandomUnlockedZone();
        int zoneId = selectedZone != null ? selectedZone.id : 1;

        Vector3 point;
        if (!TryGetRandomPointForZone(zoneId, out point))
        {
            Debug.LogWarning($"DeliveryManager: could not get random point for zone {zoneId}.");
            return;
        }

        Debug.Log($"AddDelivery: got random point {point} for zone {zoneId}");

        var job = DeliveryService.Create(itemId, itemName, point, zoneId);

        Debug.Log($"Created delivery job #{job.id} for {itemName} at {point} in zone {zoneId}");

        if (currentJob == null)
            RefreshCurrentJob();

        RefreshDeliveryPoint();
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

    public Vector3? GetCurrentTarget()
    {
        if (currentJob == null)
            return null;

        return DeliveryService.GetTargetPosition(currentJob);
    }

    private void RefreshDeliveryPoint()
    {
        if (SceneManager.GetActiveScene().name != mainSceneName)
            return;

        if (currentJob == null)
        {
            DestroyDeliveryPoint();
            return;
        }

        if (deliveryPointPrefab == null)
        {
            Debug.LogWarning("DeliveryManager: deliveryPointPrefab is not assigned.");
            return;
        }

        Vector3 gridCenter = DeliveryService.GetTargetPosition(currentJob);
        GetDeliveryPointPose(currentJob, gridCenter, out Vector3 spawnPosition, out Quaternion spawnRotation);

        if (activeDeliveryPoint == null)
        {
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, spawnPosition, spawnRotation);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
            Debug.Log($"Spawned delivery point for job #{currentJob.id} at {spawnPosition}");
            return;
        }

        DeliveryJob boundJob = activeDeliveryPoint.GetBoundJob();
        if (boundJob == null || boundJob.id != currentJob.id)
        {
            Destroy(activeDeliveryPoint.gameObject);
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, spawnPosition, spawnRotation);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
            Debug.Log($"Respawned delivery point for job #{currentJob.id} at {spawnPosition}");
            return;
        }

        if (!activeDeliveryPoint.gameObject.activeSelf)
            activeDeliveryPoint.gameObject.SetActive(true);

        activeDeliveryPoint.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
    }

    private void GetDeliveryPointPose(DeliveryJob job, Vector3 gridCenter, out Vector3 position, out Quaternion rotation)
    {
        Vector3 outward = GetOutwardBorderDirection(job);
        position = gridCenter + outward * borderOffsetDistance + Vector3.up * deliveryPointHeightOffset;
        rotation = Quaternion.LookRotation(outward, Vector3.up);
    }

    private Vector3 GetOutwardBorderDirection(DeliveryJob job)
    {
        int seed = job != null ? job.id : 0;
        int side = Mathf.Abs(seed) % 4;

        switch (side)
        {
            case 0: return Vector3.forward;
            case 1: return Vector3.right;
            case 2: return Vector3.back;
            default: return Vector3.left;
        }
    }

    private void DestroyDeliveryPoint()
    {
        if (activeDeliveryPoint != null)
        {
            Destroy(activeDeliveryPoint.gameObject);
            activeDeliveryPoint = null;
        }
    }

    public bool TryCompleteDeliveryFromPoint(DeliveryPointInteractable point, DeliveryJob job)
    {
        if (currentJob == null)
            return false;

        if (job == null || point == null)
            return false;

        if (currentJob.id != job.id)
        {
            Debug.LogWarning("DeliveryManager: attempted to complete the wrong delivery job.");
            return false;
        }

        Debug.Log($"Completed delivery #{currentJob.id} ({currentJob.itemName})");

        if (InventoryManager.Instance != null)
        {
            bool removed = InventoryManager.Instance.RemoveFirstMatchingItem(currentJob.itemId);

            if (!removed)
            {
                Debug.LogWarning("Delivery completed, but matching item was not found in inventory: " + currentJob.itemId);
            }
        }

        int zoneId = DeliveryService.GetZoneId(currentJob);
        int finalPay = DeliveryRewardService.GetFinalPay(Mathf.RoundToInt((float)basePay), zoneId);
        int finalXp = DeliveryRewardService.GetFinalXp(baseDeliveryExperience, zoneId);

        DeliveryService.Complete(currentJob.id);
        PlayerService.RewardDelivery(finalPay, finalXp);

        Debug.Log($"+€{finalPay} earned from delivery in zone {zoneId}");
        Debug.Log($"+{finalXp} XP earned from delivery in zone {zoneId}");

        if (activeDeliveryPoint != null)
        {
            Destroy(activeDeliveryPoint.gameObject);
            activeDeliveryPoint = null;
        }

        currentJob = null;
        RefreshCurrentJob();
        RefreshDeliveryPoint();
        return true;
    }

    private DeliveryZone GetRandomUnlockedZone()
    {
        var unlockedZones = ZoneService.GetUnlockedZones();

        if (unlockedZones == null || unlockedZones.Count == 0)
        {
            var fallback = DbBoot.Instance.Db.Find<DeliveryZone>(1);
            if (fallback != null)
                return fallback;

            return new DeliveryZone
            {
                id = 1,
                name = "Zone 1",
                unlockCost = 0,
                requiredLevel = 1,
                payMultiplier = 1f,
                xpMultiplier = 1f,
                startsUnlocked = 1
            };
        }

        int index = UnityEngine.Random.Range(0, unlockedZones.Count);
        return unlockedZones[index];
    }

    private bool TryGetRandomPointForZone(int zoneId, out Vector3 point)
    {
        point = Vector3.zero;

        if (DeliveryGridProvider.Instance == null)
            return false;

        object provider = DeliveryGridProvider.Instance;
        Type providerType = provider.GetType();

        MethodInfo zoneMethod = providerType.GetMethod(
            "GetRandomPointInZone",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(int) },
            null);

        if (zoneMethod != null)
        {
            object result = zoneMethod.Invoke(provider, new object[] { zoneId });
            if (result is Vector3 vectorResult)
            {
                point = vectorResult;
                return true;
            }
        }

        MethodInfo fallbackZoneMethod = providerType.GetMethod(
            "GetRandomPointForZone",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(int) },
            null);

        if (fallbackZoneMethod != null)
        {
            object result = fallbackZoneMethod.Invoke(provider, new object[] { zoneId });
            if (result is Vector3 vectorResult)
            {
                point = vectorResult;
                return true;
            }
        }

        MethodInfo oldMethod = providerType.GetMethod(
            "GetRandomPoint",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (oldMethod != null)
        {
            object result = oldMethod.Invoke(provider, null);
            if (result is Vector3 vectorResult)
            {
                point = vectorResult;
                Debug.LogWarning($"DeliveryManager: DeliveryGridProvider has no zone-aware method yet, using fallback GetRandomPoint() for zone {zoneId}.");
                return true;
            }
        }

        return false;
    }
}