using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Delivery")]
    public float completeRadius = 5f;
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

    [Header("Delivery Complete Effect")]
    [SerializeField] private ParticleSystem deliveryCompleteEffectPrefab;

    [Tooltip("How long before the spawned effect is destroyed automatically.")]
    [SerializeField] private float deliveryCompleteEffectLifetime = 3f;

    [Tooltip("Optional vertical offset for the completion particle effect.")]
    [SerializeField] private float deliveryCompleteEffectHeightOffset = 0.25f;

    [Header("Rewards")]
    [Tooltip("Base pay before zone multiplier. Usually your Zone 1 base value.")]
    [SerializeField] private int basePay = 100;

    [Tooltip("Base XP before zone multiplier. Usually your Zone 1 base value.")]
    [SerializeField] private int baseDeliveryExperience = PlayerService.ExpPerDelivery;

    [Header("Zones")]
    [Tooltip("Highest delivery zone id that can be rolled.")]
    [SerializeField] private int maxZoneId = 6;

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
        RefreshDeliveryState();
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
        if (DeliveryGridProvider.Instance == null)
            return;

        int zoneId;
        Vector3 point;

        if (!TryGetRandomUnlockedDelivery(out zoneId, out point))
            return;

        int finalPay = DeliveryRewardService.GetFinalPay(basePay, zoneId);
        int finalXp = DeliveryRewardService.GetFinalXp(baseDeliveryExperience, zoneId);

        var job = DeliveryService.Create(itemId, itemName, point, zoneId);

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

    public void RefreshDeliveryState()
    {
        RefreshCurrentJob();

        if (currentJob == null)
        {
            if (activeMarker != null)
                activeMarker.gameObject.SetActive(false);

            DestroyDeliveryPoint();
            return;
        }

        RefreshDeliveryPoint();
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
            return;

        Vector3 gridCenter = DeliveryService.GetTargetPosition(currentJob);
        GetDeliveryPointPose(currentJob, gridCenter, out Vector3 spawnPosition, out Quaternion spawnRotation);

        if (activeDeliveryPoint == null)
        {
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, spawnPosition, spawnRotation);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
            return;
        }

        DeliveryJob boundJob = activeDeliveryPoint.GetBoundJob();
        if (boundJob == null || boundJob.id != currentJob.id)
        {
            Destroy(activeDeliveryPoint.gameObject);
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, spawnPosition, spawnRotation);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
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

    private void PlayDeliveryCompleteEffect(Vector3 position)
    {
        if (deliveryCompleteEffectPrefab == null)
            return;

        Vector3 spawnPosition = position + Vector3.up * deliveryCompleteEffectHeightOffset;
        ParticleSystem spawnedEffect = Instantiate(deliveryCompleteEffectPrefab, spawnPosition, Quaternion.identity);

        spawnedEffect.Play();

        if (deliveryCompleteEffectLifetime > 0f)
            Destroy(spawnedEffect.gameObject, deliveryCompleteEffectLifetime);
    }

    public bool TryCompleteDeliveryFromPoint(DeliveryPointInteractable point, DeliveryJob job)
    {
        if (currentJob == null)
            return false;

        if (job == null || point == null)
            return false;

        if (currentJob.id != job.id)
            return false;

        if (InventoryManager.Instance != null)
        {
            bool removed = InventoryManager.Instance.RemoveFirstMatchingItem(currentJob.itemId);
        }

        int zoneId = DeliveryService.GetZoneId(currentJob);
        int finalPay = DeliveryRewardService.GetFinalPay(basePay, zoneId);
        int finalXp = DeliveryRewardService.GetFinalXp(baseDeliveryExperience, zoneId);

        Vector3 effectPosition = point.transform.position;

        DeliveryService.Complete(currentJob.id, finalPay, finalXp);
        PlayerService.RewardDelivery(finalPay, finalXp);

        PlayDeliveryCompleteEffect(effectPosition);

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

    private int GetRandomUnlockedZoneId()
    {
        List<int> unlockedZoneIds = new List<int>();

        int safeMaxZoneId = Mathf.Max(1, maxZoneId);

        for (int zoneId = 1; zoneId <= safeMaxZoneId; zoneId++)
        {
            if (IsZoneUnlockedForDeliveries(zoneId))
                unlockedZoneIds.Add(zoneId);
        }

        if (unlockedZoneIds.Count == 0)
            return 1;

        int index = Random.Range(0, unlockedZoneIds.Count);
        return unlockedZoneIds[index];
    }

    private bool IsZoneUnlockedForDeliveries(int zoneId)
    {
        if (zoneId <= 1)
            return true;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            return false;

        return ZoneService.IsZoneUnlocked(zoneId);
    }

    private bool TryGetRandomUnlockedDelivery(out int zoneId, out Vector3 point)
    {
        zoneId = 1;
        point = Vector3.zero;

        if (DeliveryGridProvider.Instance == null)
            return false;

        List<int> unlockedZoneIds = GetUnlockedZoneIds();

        if (unlockedZoneIds.Count == 0)
            unlockedZoneIds.Add(1);

        Shuffle(unlockedZoneIds);

        for (int i = 0; i < unlockedZoneIds.Count; i++)
        {
            int candidateZoneId = unlockedZoneIds[i];
            Vector3 candidatePoint = DeliveryGridProvider.Instance.GetRandomPointInZone(candidateZoneId);

            if (candidatePoint == Vector3.zero)
                continue;

            zoneId = candidateZoneId;
            point = candidatePoint;
            return true;
        }

        Vector3 fallbackPoint = DeliveryGridProvider.Instance.GetRandomPoint();

        if (fallbackPoint == Vector3.zero)
            return false;

        zoneId = 1;
        point = fallbackPoint;
        return true;
    }

    private List<int> GetUnlockedZoneIds()
    {
        List<int> unlockedZoneIds = new List<int>();

        int safeMaxZoneId = Mathf.Max(1, maxZoneId);

        for (int zoneId = 1; zoneId <= safeMaxZoneId; zoneId++)
        {
            if (IsZoneUnlockedForDeliveries(zoneId))
                unlockedZoneIds.Add(zoneId);
        }

        return unlockedZoneIds;
    }

    private void Shuffle(List<int> list)
    {
        if (list == null || list.Count <= 1)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            int swapIndex = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }
}