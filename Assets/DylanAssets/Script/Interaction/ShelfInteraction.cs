using UnityEngine;
using UnityEngine.InputSystem;

public class ShelfInteract : MonoBehaviour
{
    [Header("Item To Give")]
    public ItemData itemToGive;   // assign your open_box ItemData here

    [Header("UI Prompt")]
    public GameObject interactPromptText;

    [Header("Shelf Settings")]
    public bool canOnlyGiveOnce = true;

    private bool playerInRange = false;
    private bool alreadyUsed = false;

    void Start()
    {
        if (interactPromptText != null)
            interactPromptText.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;
        if (alreadyUsed && canOnlyGiveOnce) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            GiveItem();
        }
    }

    private void GiveItem()
    {
        if (itemToGive == null)
        {
            //debug.LogWarning("ShelfInteract: No item assigned.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            //debug.LogWarning("ShelfInteract: InventoryManager not found.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemToGive);

        if (added)
        {
            //debug.Log($"Added {itemToGive.name} to inventory from shelf.");

            alreadyUsed = true;

            if (canOnlyGiveOnce && interactPromptText != null)
                interactPromptText.SetActive(false);
        }
        else
        {
            //debug.Log("Inventory full, could not add item.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        //debug.Log("Shelf trigger entered");

        playerInRange = true;

        if (interactPromptText != null)
        {
            interactPromptText.SetActive(true);
            //debug.Log("Shelf prompt shown");
        }
        else
        {
            //debug.LogWarning("Shelf prompt not assigned");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        //debug.Log("Shelf trigger exited");

        playerInRange = false;

        if (interactPromptText != null)
            interactPromptText.SetActive(false);
    }

    
}