using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleStorageInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VehicleLink vehicleLink;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string promptRootObjectName = "VehicleStoragePrompt";

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Press E to access storage";

    private bool playerInRange;

    private void Awake()
    {
        ResolveVehicleLink();
        ResolvePromptReferences();
        SetPromptVisible(false);
    }

    private void Start()
    {
        ResolveVehicleLink();
        ResolvePromptReferences();

        if (promptText != null)
            promptText.text = promptMessage;

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (vehicleLink == null || vehicleLink.vehicleId <= 0)
            ResolveVehicleLink();

        if (promptRoot == null || promptText == null)
            ResolvePromptReferences();

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (vehicleLink == null)
        {
            Debug.LogWarning("[VehicleStorageInteract] VehicleLink missing.");
            return;
        }

        if (vehicleLink.vehicleId <= 0)
        {
            Debug.LogWarning("[VehicleStorageInteract] Invalid vehicleId.");
            return;
        }

        if (VehicleStorageUI.Instance == null)
        {
            Debug.LogWarning("[VehicleStorageInteract] VehicleStorageUI instance not found.");
            return;
        }

        VehicleStorageUI.Instance.ToggleForVehicle(vehicleLink.vehicleId);

        bool isOpenForThisVehicle =
            VehicleStorageUI.Instance.IsOpen &&
            VehicleStorageUI.Instance.CurrentVehicleId == vehicleLink.vehicleId;

        SetPromptVisible(!isOpenForThisVehicle && playerInRange);
    }

    private void ResolveVehicleLink()
    {
        if (vehicleLink != null && vehicleLink.vehicleId > 0)
            return;

        vehicleLink = GetComponent<VehicleLink>();

        if (vehicleLink == null || vehicleLink.vehicleId <= 0)
            vehicleLink = GetComponentInParent<VehicleLink>();

        if (vehicleLink == null || vehicleLink.vehicleId <= 0)
            vehicleLink = GetComponentInChildren<VehicleLink>(true);

        if (vehicleLink == null || vehicleLink.vehicleId <= 0)
        {
            var allLinks = GetComponentsInParent<VehicleLink>(true);
            for (int i = 0; i < allLinks.Length; i++)
            {
                if (allLinks[i] != null && allLinks[i].vehicleId > 0)
                {
                    vehicleLink = allLinks[i];
                    return;
                }
            }
        }
    }

    private void ResolvePromptReferences()
    {
        if (promptRoot == null)
        {
            promptRoot = FindPromptRootIncludingInactive();
        }

        if (promptRoot != null && promptText == null)
        {
            promptText = promptRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private GameObject FindPromptRootIncludingInactive()
    {
        GameObject activeMatch = GameObject.Find(promptRootObjectName);
        if (activeMatch != null)
            return activeMatch;

        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        for (int i = 0; i < allTexts.Length; i++)
        {
            var tmp = allTexts[i];
            if (tmp == null)
                continue;

            GameObject go = tmp.gameObject;
            if (go == null)
                continue;

            Transform t = go.transform;
            while (t != null)
            {
                if (t.name == promptRootObjectName)
                    return t.gameObject;

                t = t.parent;
            }
        }

        return null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        ResolveVehicleLink();
        ResolvePromptReferences();

        if (promptText != null)
            promptText.text = promptMessage;

        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        SetPromptVisible(false);

        if (VehicleStorageUI.Instance != null &&
            VehicleStorageUI.Instance.IsOpen &&
            vehicleLink != null &&
            VehicleStorageUI.Instance.CurrentVehicleId == vehicleLink.vehicleId)
        {
            VehicleStorageUI.Instance.Hide();
        }
    }
}