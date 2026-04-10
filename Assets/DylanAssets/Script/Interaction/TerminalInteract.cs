using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerminalInteract : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject managerPanel;
    [SerializeField] private TMP_Text promptText;

    [Header("Player Object")]
    [SerializeField] private GameObject playerCapsule;

    [Header("Detected Scripts (read only at runtime)")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour playerLookScript;

    private bool playerInRange;

    private void Awake()
    {
        CachePlayerScripts();

        if (managerPanel != null)
            managerPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        CachePlayerScripts();
    }

    private void Start()
    {
        LockPlayer(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (managerPanel != null && managerPanel.activeSelf)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (managerPanel == null)
            return;

        CachePlayerScripts();

        managerPanel.SetActive(true);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        LockPlayer(true);
    }

    public void ClosePanel()
    {
        if (managerPanel != null)
            managerPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(playerInRange);

        LockPlayer(false);

        RefreshPlayerUi();
    }

    private void LockPlayer(bool locked)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = !locked;

        if (playerLookScript != null)
            playerLookScript.enabled = !locked;

        Cursor.visible = locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CachePlayerScripts()
    {
        playerMovementScript = null;
        playerLookScript = null;

        if (playerCapsule == null)
            return;

        MonoBehaviour[] behaviours = playerCapsule.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;

            if (playerMovementScript == null &&
                (typeName == "PlayerMovementInside" || typeName == "PlayerMovementOutside"))
            {
                playerMovementScript = behaviour;
                continue;
            }

            if (playerLookScript == null &&
                (typeName == "PlayerLook" || typeName == "FlyoverController"))
            {
                playerLookScript = behaviour;
                continue;
            }
        }
    }

    private void RefreshPlayerUi()
    {
        if (DbBoot.Instance == null)
            return;

        PlayerService.RefreshAllUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (promptText != null && (managerPanel == null || !managerPanel.activeSelf))
            promptText.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        ClosePanel();
    }

    private void OnDisable()
    {
        LockPlayer(false);
        RefreshPlayerUi();
    }
}