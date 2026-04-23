using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [SerializeField] private GameObject gameOverScreen;

    private bool hasTriggered = false;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerGameOver()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        Time.timeScale = 0f; // pause game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}