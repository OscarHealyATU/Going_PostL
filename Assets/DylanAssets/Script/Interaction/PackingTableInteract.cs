using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PackingTableInteract : MonoBehaviour
{
    [Header("UI")]
    public GameObject packingPanel;
    public TMP_Text promptText;

    private bool _playerInRange;

    void Start()
    {
        if (packingPanel != null)
            packingPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TogglePackingUI();
    }

    private void TogglePackingUI()
    {
        if (packingPanel == null) return;

        bool newState = !packingPanel.activeSelf;
        packingPanel.SetActive(newState);

        if (promptText != null)
            promptText.gameObject.SetActive(!newState);
    }

    private void CloseUI()
    {
        if (packingPanel != null)
            packingPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void ShowPrompt()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = true;

        if (packingPanel == null || !packingPanel.activeSelf)
            ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;
        CloseUI();
    }
}