using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialWhiteboardInteract : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialPanelController tutorialPanel;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Press E for tutorial";

    [Header("Interaction")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private bool requireTrigger = true;

    private bool playerInRange;

    private void Awake()
    {
        if (promptText != null)
            promptText.text = promptMessage;

        SetPromptVisible(false);

        if (tutorialPanel != null)
            tutorialPanel.OnPanelStateChanged += HandlePanelStateChanged;
    }

    private void OnDestroy()
    {
        if (tutorialPanel != null)
            tutorialPanel.OnPanelStateChanged -= HandlePanelStateChanged;
    }

    private void Update()
    {
        if (tutorialPanel == null)
            return;

        if (tutorialPanel.IsOpen)
            return;

        if (requireTrigger && !playerInRange)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            tutorialPanel.OpenPanel();
        }
    }

    private void HandlePanelStateChanged(bool open)
    {
        if (open)
        {
            SetPromptVisible(false);
        }
        else
        {
            SetPromptVisible(playerInRange);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (tutorialPanel == null || !tutorialPanel.IsOpen)
            SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptText != null)
            promptText.gameObject.SetActive(visible);
    }
}