using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TerminalInteract : MonoBehaviour
{
    [Header("UI")]
    public GameObject vehicleShopPanel; // the panel to open/close
    public TMP_Text promptText;         // "Press E..." text

    private bool _playerInRange;

    void Start()
    {
        if (vehicleShopPanel != null)
            vehicleShopPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ToggleShop();
    }

    private void ToggleShop()
    {
        if (vehicleShopPanel == null) return;

        bool newState = !vehicleShopPanel.activeSelf;
        vehicleShopPanel.SetActive(newState);

        // ✅ When opening, rebuild list from DB
        if (newState)
        {
            var shop = vehicleShopPanel.GetComponentInChildren<VehicleShopUI>(true);
            if (shop != null)
                shop.Rebuild();
        }

        if (promptText != null)
            promptText.gameObject.SetActive(!newState);
    }

    private void CloseShop()
    {
        if (vehicleShopPanel != null)
            vehicleShopPanel.SetActive(false);

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

        if (vehicleShopPanel == null || !vehicleShopPanel.activeSelf)
            ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;
        CloseShop();
    }
}