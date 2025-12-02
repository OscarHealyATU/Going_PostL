using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class TerminalInteract : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad;

    [Header("UI Prompt")]
    public GameObject interactPromptText;

    private bool playerInRange = false;
    private Keyboard keyboard;

    void Start()
    {
        keyboard = Keyboard.current;
    }

    void Update()
    {
        if (playerInRange && keyboard.eKey.wasPressedThisFrame)
        {
            Debug.Log($"✅ E pressed! Attempting to load scene: {sceneToLoad}");

            if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError($"❌ Scene '{sceneToLoad}' cannot be loaded. Check spelling and Build Settings!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactPromptText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactPromptText.SetActive(false);
        }
    }
}
