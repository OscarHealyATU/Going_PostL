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

    private bool playerInRange = false;
    private Transform playerTransform;

    void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
    }

    void OnEnable()
    {
        HidePrompt();
        playerInRange = false;
        playerTransform = null;

        StartCoroutine(RefreshTriggerStateAfterSceneLoad());
    }

    IEnumerator RefreshTriggerStateAfterSceneLoad()
    {
        // Let scene objects and player finish spawning first
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

    void Update()
    {
        var kb = Keyboard.current;
        if (!playerInRange || kb == null || !kb.eKey.wasPressedThisFrame)
            return;

        Debug.Log($"✅ Interact: E pressed on '{gameObject.name}'. Loading scene: {sceneToLoad}");

        if (saveReturnPointBeforeSceneLoad && playerTransform != null)
        {
            PlayerService.SaveReturnPoint(playerTransform.position, playerTransform.eulerAngles.y);
            Debug.Log($"📌 Saved return position: {playerTransform.position} yaw={playerTransform.eulerAngles.y}");
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"❌ Scene '{sceneToLoad}' cannot be loaded. Check spelling and Build Settings!");
            return;
        }

        HidePrompt();
        playerInRange = false;

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(sceneToLoad);
        else
            SceneManager.LoadScene(sceneToLoad);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        playerTransform = other.transform;
        ShowPrompt();

        Debug.Log($"🟦 Interact: Player entered trigger '{gameObject.name}'.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        playerTransform = null;
        HidePrompt();

        Debug.Log($"🟥 Interact: Player exited trigger '{gameObject.name}'.");
    }

    void OnDisable()
    {
        HidePrompt();
    }

    void OnDestroy()
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

        // If closest point is basically the same as player position,
        // the player is inside or intersecting the trigger.
        return Vector3.SqrMagnitude(point - closest) < 0.0001f;
    }
}