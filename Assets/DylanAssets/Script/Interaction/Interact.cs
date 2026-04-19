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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
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
        Keyboard kb = Keyboard.current;
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

        // Normal warehouses
        if (warehouseIdentity != null && warehouseIdentity.WarehouseId > 0)
        {
            bool success = WarehouseService.SetLastInteractedWarehouse(warehouseIdentity.WarehouseId);

            if (success)
            {
                Debug.Log($"[Interact] Current warehouse set from WarehouseIdentity ID {warehouseIdentity.WarehouseId}");
                return;
            }
        }

        // Fallback for starter warehouse
        Debug.Log("[Interact] No warehouse identity found. Falling back to starter warehouse ID 1.");

        bool starterSuccess = WarehouseService.SetLastInteractedWarehouse(1);

        if (!starterSuccess)
        {
            Debug.LogWarning("[Interact] Failed to set starter warehouse as current.");
        }
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

        // Debug.Log($"🟦 Interact: Player entered trigger '{gameObject.name}'.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        playerTransform = null;
        HidePrompt();

        // Debug.Log($"🟥 Interact: Player exited trigger '{gameObject.name}'.");
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