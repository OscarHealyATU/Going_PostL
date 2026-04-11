using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleStorageUI : MonoBehaviour
{
    public static VehicleStorageUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;

    [Header("Slots")]
    [SerializeField] private Transform slotsRoot;
    [SerializeField] private VehicleStorageSlotUI slotPrefab;

    [Header("Refresh")]
    [SerializeField] private bool rebuildOnOpen = true;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Player Locking")]
    [SerializeField] private GameObject playerCapsuleObject;
    [SerializeField] private newWalkController walkController;
    [SerializeField] private FlyoverController flyoverController;

    private readonly List<VehicleStorageSlotUI> spawnedSlots = new List<VehicleStorageSlotUI>();

    private int currentVehicleId = -1;
    private Vehicle currentVehicle;
    private VehicleType currentVehicleType;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public int CurrentVehicleId => currentVehicleId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        ResolvePlayerControlReferences();

        if (panelRoot != gameObject)
            panelRoot.SetActive(false);

        Debug.Log("[VehicleStorageUI] Instance ready.");
    }

    public void ShowForVehicle(int vehicleId)
    {
        if (DbBoot.Instance == null)
        {
            Debug.LogWarning("[VehicleStorageUI] DbBoot not available.");
            return;
        }

        var db = DbBoot.Instance.Db;

        currentVehicle = db.Find<Vehicle>(vehicleId);
        if (currentVehicle == null)
        {
            Debug.LogWarning($"[VehicleStorageUI] Vehicle not found for id={vehicleId}");
            return;
        }

        currentVehicleType = db.Find<VehicleType>(currentVehicle.vehicleTypeId);
        if (currentVehicleType == null)
        {
            Debug.LogWarning($"[VehicleStorageUI] VehicleType not found for id={currentVehicle.vehicleTypeId}");
            return;
        }

        currentVehicleId = vehicleId;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        SetPlayerControlLocked(true);

        if (rebuildOnOpen) 
            RebuildSlots();

        RefreshUI();
        ResetScrollToTopImmediate();
        StartCoroutine(ResetScrollToTopNextFrame());
    }

    public void Hide()
    {
        currentVehicleId = -1;
        currentVehicle = null;
        currentVehicleType = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        SetPlayerControlLocked(false);
    }

    public void ToggleForVehicle(int vehicleId)
    {
        if (IsOpen && currentVehicleId == vehicleId)
        {
            Hide();
            return;
        }

        ShowForVehicle(vehicleId);
    }

    public void RefreshUI()
    {
        if (!IsOpen || currentVehicleId <= 0 || DbBoot.Instance == null)
            return;

        var db = DbBoot.Instance.Db;

        currentVehicle = db.Find<Vehicle>(currentVehicleId);
        if (currentVehicle == null)
        {
            Hide();
            return;
        }

        currentVehicleType = db.Find<VehicleType>(currentVehicle.vehicleTypeId);
        if (currentVehicleType == null)
        {
            Hide();
            return;
        }

        int capacity = Mathf.Max(0, currentVehicleType.storageCapacity);

        if (titleText != null)
        {
            int used = VehicleStorageService.GetUsedSlotCount(currentVehicleId);
            titleText.text = $"{currentVehicleType.name} Storage";
        }

        if (spawnedSlots.Count != capacity)
            RebuildSlots();

        Dictionary<int, StoredDelivery> storedBySlot = VehicleStorageService.GetStoredDeliveries(currentVehicleId)
            .ToDictionary(x => x.slotIndex, x => x);

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            var slotUi = spawnedSlots[i];
            if (slotUi == null)
                continue;

            if (storedBySlot.TryGetValue(i, out StoredDelivery stored))
            {
                Sprite iconSprite = GetItemIcon(stored.itemId);
                slotUi.Setup(this, currentVehicleId, i, stored, iconSprite);
            }
            else
            {
                slotUi.SetEmpty(this, currentVehicleId, i);
            }
        }
    }

    public void RebuildSlots()
    {
        ClearSpawnedSlots();

        if (slotsRoot == null)
        {
            Debug.LogWarning("[VehicleStorageUI] slotsRoot is not assigned.");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogWarning("[VehicleStorageUI] slotPrefab is not assigned.");
            return;
        }

        if (currentVehicleType == null)
            return;

        int capacity = Mathf.Max(0, currentVehicleType.storageCapacity);

        for (int i = 0; i < capacity; i++)
        {
            VehicleStorageSlotUI slot = Instantiate(slotPrefab, slotsRoot);
            slot.name = $"VehicleStorageSlot_{i}";
            slot.SetEmpty(this, currentVehicleId, i);
            spawnedSlots.Add(slot);
        }

        ForceLayoutRefresh();
    }

    public void OnStorageSlotClicked(VehicleStorageSlotUI slotUi)
    {
        if (slotUi == null)
            return;

        if (slotUi.StoredDelivery == null)
            return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[VehicleStorageUI] InventoryManager not available.");
            return;
        }

        bool moved = InventoryManager.Instance.TryMoveStoredDeliveryToInventory(slotUi.StoredDelivery.id, out string message);
        Debug.Log($"[VehicleStorageUI] {message}");

        if (moved)
        {
            RefreshUI();
            ForceLayoutRefresh();
        }
    }

    private void ClearSpawnedSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();
    }

    private Sprite GetItemIcon(string itemKey)
    {
        if (ItemCatalog.Instance == null || string.IsNullOrWhiteSpace(itemKey))
            return null;

        ItemData item = ItemCatalog.Instance.GetByKey(itemKey);
        return item != null ? item.icon : null;
    }

    private void ForceLayoutRefresh()
    {
        if (slotsRoot == null)
            return;

        RectTransform rect = slotsRoot as RectTransform;
        if (rect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        Canvas.ForceUpdateCanvases();
    }

    private void ResetScrollToTopImmediate()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private IEnumerator ResetScrollToTopNextFrame()
    {
        yield return null;

        if (scrollRect == null)
            yield break;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ResolvePlayerControlReferences()
    {
        if (playerCapsuleObject != null)
        {
            if (walkController == null)
                walkController = playerCapsuleObject.GetComponent<newWalkController>();

            if (flyoverController == null)
                flyoverController = playerCapsuleObject.GetComponent<FlyoverController>();

            return;
        }

        if (walkController == null)
            walkController = FindFirstObjectByType<newWalkController>();

        if (flyoverController == null)
            flyoverController = FindFirstObjectByType<FlyoverController>();

        if (playerCapsuleObject == null)
        {
            if (walkController != null)
                playerCapsuleObject = walkController.gameObject;
            else if (flyoverController != null)
                playerCapsuleObject = flyoverController.gameObject;
        }
    }

    private void SetPlayerControlLocked(bool locked)
    {
        ResolvePlayerControlReferences();

        if (walkController != null)
            walkController.enabled = !locked;

        if (flyoverController != null)
            flyoverController.enabled = !locked;

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }
}