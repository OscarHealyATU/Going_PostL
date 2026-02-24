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

    private bool playerInRange = false;
    private Transform playerTransform;

    void Update()
    {
        var kb = Keyboard.current;
        if (!playerInRange || kb == null || !kb.eKey.wasPressedThisFrame)
            return;

        Debug.Log($"✅ Interact: E pressed on '{gameObject.name}'. Loading scene: {sceneToLoad}");

        // ✅ Save player position BEFORE switching scenes (only if enabled)
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

        // ✅ Smooth transition if SceneFader exists, otherwise fallback
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

        if (interactPromptText != null)
            interactPromptText.SetActive(true);

        Debug.Log($"🟦 Interact: Player entered trigger '{gameObject.name}'.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        playerTransform = null;

        if (interactPromptText != null)
            interactPromptText.SetActive(false);

        Debug.Log($"🟥 Interact: Player exited trigger '{gameObject.name}'.");
    }
}