using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class VehicleLocationSaver : MonoBehaviour
{
    private carController controller;
    private VehicleLink vehicleLink;

    private bool previousDrivenState;

    private void Awake()
    {
        controller = GetComponent<carController>();
        vehicleLink = GetComponent<VehicleLink>();

        if (controller == null)
            Debug.LogWarning($"[VehicleLocationSaver] No carController found on '{name}'");

        if (vehicleLink == null)
            Debug.LogWarning($"[VehicleLocationSaver] No VehicleLink found on '{name}'");
    }

    private void Start()
    {
        if (controller != null)
            previousDrivenState = controller.isBeingDriven;
    }

    private void Update()
    {
        if (controller == null || vehicleLink == null)
            return;

        bool currentDrivenState = controller.isBeingDriven;

        if (previousDrivenState && !currentDrivenState)
        {
            SaveCurrentLocation();
        }

        previousDrivenState = currentDrivenState;
    }

    private void OnDisable()
    {
        SaveIfNotBeingDriven();
    }

    private void OnDestroy()
    {
        SaveIfNotBeingDriven();
    }

    private void OnApplicationQuit()
    {
        SaveIfNotBeingDriven();
    }

    private void SaveIfNotBeingDriven()
    {
        if (controller == null || vehicleLink == null)
            return;

        if (!controller.isBeingDriven)
            SaveCurrentLocation();
    }

    public void SaveCurrentLocation()
    {
        if (vehicleLink == null || vehicleLink.vehicleId <= 0)
            return;

        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            Debug.LogWarning("[VehicleLocationSaver] DB unavailable, cannot save vehicle location.");
            return;
        }

        var db = DbBoot.Instance.Db;
        var vehicle = db.Find<Vehicle>(vehicleLink.vehicleId);

        if (vehicle == null)
        {
            Debug.LogWarning($"[VehicleLocationSaver] Vehicle row not found for vehicleId={vehicleLink.vehicleId}");
            return;
        }

        Vector3 pos = transform.position;
        float yaw = transform.eulerAngles.y;
        string activeScene = SceneManager.GetActiveScene().name;

        vehicle.hasSavedLocation = 1;
        vehicle.savedScene = activeScene;
        vehicle.savedX = pos.x;
        vehicle.savedY = pos.y;
        vehicle.savedZ = pos.z;
        vehicle.savedYaw = yaw;

        db.Update(vehicle);

        Debug.Log($"[VehicleLocationSaver] Saved vehicle {vehicle.id} at ({pos.x:0.00}, {pos.y:0.00}, {pos.z:0.00}) yaw {yaw:0.0} in scene '{activeScene}'");
    }

    [ContextMenu("Save Vehicle Location Now")]
    private void SaveNowFromContextMenu()
    {
        SaveCurrentLocation();
    }
}