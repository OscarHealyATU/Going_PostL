using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad;

    [Header("Save Return Point Before Loading?")]
    public bool saveReturnPointBeforeSceneLoad = false;

    [Header("UI Prompt")]
    public GameObject interactPromptText;

    [Header("Trigger")]
    public Collider triggerCollider;

    [Header("Warehouse Tracking")]
    [SerializeField] private bool setCurrentWarehouseOnInteract = false;
    [SerializeField] private string warehouseZoneName;
    [SerializeField] private int warehouseTileX = -1;
    [SerializeField] private int warehouseTileZ = -1;

    [Header("Runtime Identity")]
    [SerializeField] private WarehouseIdentity warehouseIdentity;

    private bool playerInRange = false;
    private Transform playerTransform;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        CacheWarehouseIdentity();
    }

    private void OnEnable()
    {
        HidePrompt();
        playerInRange = false;
        playerTransform = null;

        StartCoroutine(RefreshTriggerStateAfterSceneLoad());
    }

    private IEnumerator RefreshTriggerStateAfterSceneLoad()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || triggerCollider == null)
            yield break;

        if (IsPlayerInsideTrigger(player.transform))
        {
            playerTransform = player.transform;
            playerInRange = true;
            ShowPrompt();
            Debug.Log($"🟨 Interact: Player was already inside trigger '{gameObject.name}' after scene load.");
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (!playerInRange || kb == null || !kb.eKey.wasPressedThisFrame)
            return;

        Debug.Log($"✅ Interact: E pressed on '{gameObject.name}'. Loading scene: {sceneToLoad}");

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"❌ Scene '{sceneToLoad}' cannot be loaded. Check spelling and Build Settings!");
            return;
        }

        if (setCurrentWarehouseOnInteract)
            TrySetCurrentWarehouse();

        if (saveReturnPointBeforeSceneLoad && playerTransform != null)
        {
            PlayerService.SaveReturnPoint(playerTransform.position, playerTransform.eulerAngles.y);
            Debug.Log($"📌 Saved return position: {playerTransform.position} yaw={playerTransform.eulerAngles.y}");
        }

        HidePrompt();
        playerInRange = false;

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(sceneToLoad);
        else
            SceneManager.LoadScene(sceneToLoad);
    }

    private void TrySetCurrentWarehouse()
    {
        CacheWarehouseIdentity();

        if (warehouseIdentity != null && warehouseIdentity.WarehouseId > 0)
        {
            bool successById = WarehouseService.SetLastInteractedWarehouse(warehouseIdentity.WarehouseId);

            if (successById)
            {
                Debug.Log($"[Interact] Current warehouse set from WarehouseIdentity ID {warehouseIdentity.WarehouseId}");
                return;
            }

            Debug.LogWarning($"[Interact] WarehouseIdentity found, but DB update failed for warehouse ID {warehouseIdentity.WarehouseId}.");
        }

        if (string.IsNullOrWhiteSpace(warehouseZoneName))
        {
            Debug.LogWarning("[Interact] No WarehouseIdentity found and warehouseZoneName is not set.");
            return;
        }

        if (warehouseTileX < 0 || warehouseTileZ < 0)
        {
            Debug.LogWarning("[Interact] Warehouse tile coordinates are invalid.");
            return;
        }

        bool success = WarehouseService.SetLastInteractedWarehouse(
            warehouseZoneName,
            warehouseTileX,
            warehouseTileZ
        );

        if (!success)
        {
            Debug.LogWarning(
                $"[Interact] Failed to set current warehouse for {warehouseZoneName} {warehouseTileX}, {warehouseTileZ}"
            );
            return;
        }

        Debug.Log($"[Interact] Current warehouse set to {warehouseZoneName} {warehouseTileX}, {warehouseTileZ}");
    }

    private void CacheWarehouseIdentity()
    {
        if (warehouseIdentity != null)
            return;

        warehouseIdentity = GetComponent<WarehouseIdentity>();

        if (warehouseIdentity == null)
            warehouseIdentity = GetComponentInParent<WarehouseIdentity>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        playerTransform = other.transform;
        ShowPrompt();

        Debug.Log($"🟦 Interact: Player entered trigger '{gameObject.name}'.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        playerTransform = null;
        HidePrompt();

        Debug.Log($"🟥 Interact: Player exited trigger '{gameObject.name}'.");
    }

    private void OnDisable()
    {
        HidePrompt();
    }

    private void OnDestroy()
    {
        HidePrompt();
    }

    private void ShowPrompt()
    {
        if (interactPromptText != null)
            interactPromptText.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactPromptText != null)
            interactPromptText.SetActive(false);
    }

    private bool IsPlayerInsideTrigger(Transform player)
    {
        if (triggerCollider == null || player == null)
            return false;

        Vector3 point = player.position;
        Vector3 closest = triggerCollider.ClosestPoint(point);

        return Vector3.SqrMagnitude(point - closest) < 0.0001f;
    }
}