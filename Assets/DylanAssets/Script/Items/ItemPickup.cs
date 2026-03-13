using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickup : MonoBehaviour
{
    private bool playerInRange = false;

    void Update()
    {
        if (!playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    void TryPickup()
    {
        WorldItem worldItem = GetComponent<WorldItem>();

        if (worldItem == null || worldItem.itemData == null)
        {
            Debug.LogWarning("ItemPickup: Missing WorldItem or ItemData.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("ItemPickup: InventoryManager not found.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(worldItem.itemData);

        if (added)
        {
            if (InteractionPromptUI.Instance != null)
                InteractionPromptUI.Instance.Hide();

            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory is full.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.Show("Press E to pick up");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.Hide();
    }
}