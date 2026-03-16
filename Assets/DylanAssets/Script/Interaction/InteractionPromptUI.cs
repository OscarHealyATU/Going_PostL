using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("References")]
    public GameObject promptRoot;
    public TMP_Text promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Hide();
    }

    public void Show(string message)
    {
        if (promptRoot != null)
            promptRoot.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    public void Hide()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }
}