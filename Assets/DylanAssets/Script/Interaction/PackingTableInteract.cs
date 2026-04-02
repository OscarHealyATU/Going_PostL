using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PackingTableInteract : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject packingPanel;
    [SerializeField] private TMP_Text promptText;

    [Header("Player")]
    [SerializeField] private PlayerMovementInside playerMovement;
    [SerializeField] private PlayerLook playerLook;

    private bool playerInRange;
    private bool uiOpen;

    private void Start()
    {
        if (packingPanel != null)
            packingPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        SetGameplayLocked(false);
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (uiOpen)
                CloseUI();
            else
                OpenUI();
        }
    }

    public void OpenUI()
    {
        uiOpen = true;

        if (packingPanel != null)
            packingPanel.SetActive(true);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        SetGameplayLocked(true);
    }

    public void CloseUI()
    {
        uiOpen = false;

        if (packingPanel != null)
            packingPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(playerInRange);

        SetGameplayLocked(false);
    }

    private void SetGameplayLocked(bool locked)
    {
        if (playerMovement != null)
            playerMovement.enabled = !locked;

        if (playerLook != null)
            playerLook.enabled = !locked;

        if (locked)
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (!uiOpen && promptText != null)
            promptText.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        CloseUI();

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }
}