using UnityEngine;

public class PlayerLookOutside : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("State")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float pitch = 0f;
    private bool cursorLocked = true;

    private void Start()
    {
        if (cameraPivot == null)
            cameraPivot = GetComponentInChildren<Camera>()?.transform;

        Vector3 camEuler = cameraPivot != null ? cameraPivot.localEulerAngles : Vector3.zero;
        pitch = NormalizeAngle(camEuler.x);

        if (lockCursorOnStart)
            SetCursorLocked(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(!cursorLocked);

        if (!cursorLocked || cameraPivot == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void ForceLook(float worldYaw, float cameraPitch = 0f)
    {
        pitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}