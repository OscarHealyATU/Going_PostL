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
    public float deliveryPointHeightOffset = 0f;

    [SerializeField] private double basePay = 100.0;

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

        Vector3 point = DeliveryGridProvider.Instance.GetRandomPoint();
        Debug.Log("AddDelivery: got random point " + point);

        var job = DeliveryService.Create(itemId, itemName, point);

        Debug.Log($"Created delivery job #{job.id} for {itemName} at {point}");

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

        Vector3 target = DeliveryService.GetTargetPosition(currentJob) + Vector3.up * deliveryPointHeightOffset;

        if (activeDeliveryPoint == null)
        {
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, target, Quaternion.identity);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
            Debug.Log($"Spawned delivery point for job #{currentJob.id} at {target}");
            return;
        }

        DeliveryJob boundJob = activeDeliveryPoint.GetBoundJob();
        if (boundJob == null || boundJob.id != currentJob.id)
        {
            Destroy(activeDeliveryPoint.gameObject);
            activeDeliveryPoint = Instantiate(deliveryPointPrefab, target, Quaternion.identity);
            activeDeliveryPoint.Initialize(currentJob, player, completeRadius, playerTag);
            Debug.Log($"Respawned delivery point for job #{currentJob.id} at {target}");
            return;
        }

        if (!activeDeliveryPoint.gameObject.activeSelf)
            activeDeliveryPoint.gameObject.SetActive(true);

        activeDeliveryPoint.transform.position = target;
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

        DeliveryService.Complete(currentJob.id);

        var player = PlayerService.Get();
        PlayerService.SetMoney(player.money + basePay);
        Debug.Log($"+€{basePay} earned from delivery");

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
}