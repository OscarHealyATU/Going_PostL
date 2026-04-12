using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BinInteract : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private BinUI binUI;
    [SerializeField] private TMP_Text promptText;

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Press E to open bin";

    private bool playerInRange;

    private void Start()
    {
        if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (binUI != null)
            {
                if (binUI.IsOpen)
                    binUI.Close();
                else
                    binUI.Open();
            }

            RefreshPrompt();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        RefreshPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (binUI != null && binUI.IsOpen)
            binUI.Close();

        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptText == null)
            return;

        bool show = playerInRange && (binUI == null || !binUI.IsOpen);
        promptText.gameObject.SetActive(show);
    }
}