using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject menuPanel;

    [Header("Player")]
    [SerializeField] private GameObject playerObject;

    [Header("Optional Camera Root")]
    [SerializeField] private GameObject cameraObject;

    [Header("Pause Settings")]
    [SerializeField] private bool pauseTimeScale = true;

    private MonoBehaviour[] playerBehaviours;
    private MonoBehaviour[] cameraBehaviours;
    private bool isPaused;

    private void Awake()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        CacheBehaviours();
        ApplyPauseState(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void CacheBehaviours()
    {
        if (playerObject != null)
            playerBehaviours = playerObject.GetComponents<MonoBehaviour>();

        if (cameraObject != null)
            cameraBehaviours = cameraObject.GetComponents<MonoBehaviour>();
    }

    public void TogglePause()
    {
        ApplyPauseState(!isPaused);
    }

    public void ResumeGame()
    {
        ApplyPauseState(false);
    }

    public void QuitToMainMenu()
    {
        if (playerObject != null)
        {
            Vector3 pos = playerObject.transform.position;
            float yaw = playerObject.transform.eulerAngles.y;
            string currentScene = SceneManager.GetActiveScene().name;

            PlayerService.SaveResumePoint(currentScene, pos, yaw);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void PauseGame()
    {
        ApplyPauseState(true);
    }

    private void ApplyPauseState(bool paused)
    {
        isPaused = paused;

        if (menuPanel != null)
            menuPanel.SetActive(paused);

        if (pauseTimeScale)
            Time.timeScale = paused ? 0f : 1f;

        SetGameplayScriptsEnabled(!paused);
        SetCursor(paused);
    }

    private void SetGameplayScriptsEnabled(bool enabledState)
    {
        SetMatchingScripts(playerBehaviours, enabledState);
        SetMatchingScripts(cameraBehaviours, enabledState);
    }

    private void SetMatchingScripts(MonoBehaviour[] behaviours, bool enabledState)
    {
        if (behaviours == null) return;

        foreach (var behaviour in behaviours)
        {
            if (behaviour == null) continue;
            if (behaviour == this) continue;

            string typeName = behaviour.GetType().Name;

            // Add any other controller script names you use here
            if (typeName == "PlayerMovementOutside" ||
                typeName == "PlayerLookOutside" ||
                typeName == "newWalkController" ||
                typeName == "FlyoverController")
            {
                behaviour.enabled = enabledState;
            }
        }
    }

    private void SetCursor(bool paused)
    {
        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}