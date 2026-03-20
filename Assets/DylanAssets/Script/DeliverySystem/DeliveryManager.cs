using UnityEngine;
using UnityEngine.SceneManagement;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Delivery")]
    public float completeRadius = 4f;
    public string mainSceneName = "Main";

    [Header("Optional world marker")]
    public Transform activeMarker;

    private DeliveryJob currentJob;

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
        TryFindPlayer();
        RefreshCurrentJob();
    }

    private void Update()
    {
        TryFindPlayer();

        if (currentJob == null || player == null)
            return;

        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            if (activeMarker != null)
                activeMarker.gameObject.SetActive(false);
            return;
        }

        Vector3 target = DeliveryService.GetTargetPosition(currentJob);

        if (activeMarker != null)
        {
            activeMarker.gameObject.SetActive(true);
            activeMarker.position = target;
        }

        float dist = Vector3.Distance(player.position, target);
        if (dist <= completeRadius)
            CompleteCurrentDelivery();
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
        if (currentJob == null)
            return;

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

        currentJob = null;
        RefreshCurrentJob();
    }

    public Vector3? GetCurrentTarget()
    {
        if (currentJob == null)
            return null;

        return DeliveryService.GetTargetPosition(currentJob);
    }
}