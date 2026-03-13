using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RandomItemSpawnerWorld : MonoBehaviour
{
    [Header("Item Data")]
    public List<ItemData> spawnableItems = new List<ItemData>();

    [Header("Spawn")]
    public Transform spawnPoint;
    public float cooldownSeconds = 0.5f;

    [Header("Optional Feedback")]
    public TMP_Text feedbackText;

    private float _nextUseTime;
    private GameObject _currentSpawnedItem;
    private bool _playerInRange;

    private void Update()
    {
        if (!_playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            SpawnItem();
        }
    }

    private void SpawnItem()
    {
        if (Time.time < _nextUseTime) return;
        _nextUseTime = Time.time + cooldownSeconds;

        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.Hide();

        if (_currentSpawnedItem != null)
        {
            SetFeedback("An item is already waiting.");
            return;
        }

        if (spawnableItems == null || spawnableItems.Count == 0)
        {
            SetFeedback("No spawnable items configured.");
            return;
        }

        ItemData chosenItem = spawnableItems[Random.Range(0, spawnableItems.Count)];

        if (chosenItem == null)
        {
            SetFeedback("Chosen ItemData is null.");
            return;
        }

        if (chosenItem.worldPrefab == null)
        {
            SetFeedback("Item has no world prefab: " + chosenItem.itemName);
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        _currentSpawnedItem = Instantiate(chosenItem.worldPrefab, point.position, point.rotation);

        WorldItem worldItem = _currentSpawnedItem.GetComponent<WorldItem>();
        if (worldItem == null)
            worldItem = _currentSpawnedItem.AddComponent<WorldItem>();

        worldItem.itemData = chosenItem;

        ItemPickup pickup = _currentSpawnedItem.GetComponent<ItemPickup>();
        if (pickup == null)
            pickup = _currentSpawnedItem.AddComponent<ItemPickup>();

        SpawnedItemInstance instance = _currentSpawnedItem.GetComponent<SpawnedItemInstance>();
        if (instance == null)
            instance = _currentSpawnedItem.AddComponent<SpawnedItemInstance>();

        instance.ownerSpawner = this;

        SetFeedback("Spawned " + chosenItem.itemName + ".");
    }

    public void ClearCurrentSpawnedReference(GameObject obj)
    {
        if (_currentSpawnedItem == obj)
            _currentSpawnedItem = null;
    }

    private void SetFeedback(string message)
    {
        Debug.Log("[RandomItemSpawnerWorld] " + message);
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = true;

        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.Show("Press E to spawn item");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;

        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.Hide();
    }
}