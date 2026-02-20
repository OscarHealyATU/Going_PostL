using UnityEngine;
using UnityEngine.InputSystem;

// A simple flyover camera controller using the new Input System.
public class FlyoverController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float shiftMultiplier = 3f;
    public float scrollSensitivity = 2f;
    [Tooltip("How quickly the camera accelerates/decelerates. Lower = smoother.")]
    [Range(1f, 20f)]
    public float moveSmoothness = 12f;

    [Header("Look")]
    public float mouseSensitivity = 1f;
    public bool invertY = false;
    [Tooltip("How quickly the camera catches up to mouse input. Lower = smoother.")]
    [Range(5f, 30f)]
    public float lookSmoothness = 12f;

    private float roteX;
    private float roteY;
    private float smoothRoteX;
    private float smoothRoteY;
    private Vector3 curVelocity;
    private bool cursorIsLocked = true;

    private Keyboard keybrd;
    private Mouse mouse;

    void Start()
    {
        LockCursor(true);
        Vector3 euler = transform.eulerAngles;
        roteY = euler.y;
        roteX = euler.x;
        smoothRoteX = roteX;
        smoothRoteY = roteY;
    }

    void Update()
    {
        keybrd = Keyboard.current;
        mouse = Mouse.current;
        if (keybrd == null || mouse == null) return;

        HandleCursorToggle();

        if (cursorIsLocked)
        {
            HandleLook();
            HandleMovement();
            HandleScrollSpeed();
        }
    }

    void HandleLook()
    {
        Vector2 delta = mouse.delta.ReadValue();
        float mx = delta.x * mouseSensitivity * 0.1f;
        float my = delta.y * mouseSensitivity * 0.1f;

        // Update target rotation instantly
        roteY += mx;
        roteX += invertY ? my : -my;
        roteX = Mathf.Clamp(roteX, -90f, 90f);

        // Smoothly interpolate toward target rotation
        smoothRoteX = Mathf.Lerp(smoothRoteX, roteX, lookSmoothness * Time.deltaTime);
        smoothRoteY = Mathf.Lerp(smoothRoteY, roteY, lookSmoothness * Time.deltaTime);

        transform.rotation = Quaternion.Euler(smoothRoteX, smoothRoteY, 0f);
    }

    void HandleMovement()
    {
        float speed = moveSpeed;
        if (keybrd.leftShiftKey.isPressed) speed *= shiftMultiplier;

        Vector3 targetDir = Vector3.zero;

        if (keybrd.wKey.isPressed) targetDir += transform.forward;
        if (keybrd.sKey.isPressed) targetDir -= transform.forward;
        if (keybrd.dKey.isPressed) targetDir += transform.right;
        if (keybrd.aKey.isPressed) targetDir -= transform.right;
        if (keybrd.spaceKey.isPressed) targetDir += Vector3.up;
        if (keybrd.leftCtrlKey.isPressed) targetDir -= Vector3.up;

        Vector3 targetVelocity = targetDir.normalized * speed;
        curVelocity = Vector3.Lerp(curVelocity, targetVelocity, moveSmoothness * Time.deltaTime);

        transform.position += curVelocity * Time.deltaTime;
    }

    void HandleScrollSpeed()
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f) moveSpeed = Mathf.Max(1f, moveSpeed + scroll * scrollSensitivity * 0.01f);
    }

    void HandleCursorToggle()
    {
        if (keybrd.escapeKey.wasPressedThisFrame) LockCursor(!cursorIsLocked);
    }

    void LockCursor(bool locked)
    {
        cursorIsLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}