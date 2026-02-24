using System;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CharacterController))]
public class newWalkController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    [Header("Walk Settings")]
    public float walkSpeed = 5f;
    public float runMult = 2f;
    [Header("Smoothness Settings")]
    [Tooltip("lower = smoother")]
    [Range(5f, 30f)]
    public float moveSmoothness = 8f;
    [Range(5f, 30f)]
    public float lookSmoothness = 12f;
    [Header("Misc Settings")]
    public float jump = 7f;
    public float gravity = -20f;
    public float mouseSens = 2f;
    
    // Private variables
    private CharacterController charController;
    private float roteX;
    private float roteY;
    private float smoothRoteX;
    private float smoothRoteY;
    private Vector3 curVelocity;
    private float upVelocity; 

    private Mouse mouse;
    private Keyboard keybrd;
    private bool cursorIsLocked = true;

    void Start()
    {
        LockCursor(true);
        charController = GetComponent<CharacterController>();
        mouse = Mouse.current;
        keybrd = Keyboard.current;

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>()?.transform;

        LockCursor(true);
        Vector3 euler = transform.eulerAngles;
        roteY = euler.y;
        // makes sure the camera doesn't snap to 0 rotation on start up
        roteX = playerCamera != null ? playerCamera.localEulerAngles.x : 0f;
    }

    // Update is called once per frame
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
        }
        
    }

    private void HandleLook()
    {
        Vector2 delta = mouse.delta.ReadValue();
        float mx = delta.x * mouseSens * 0.1f;
        float my = delta.y * mouseSens * 0.1f;
        roteY += mx;
        
    }

    private void HandleMovement()
    {
        throw new NotImplementedException();
    }

    private void HandleCursorToggle()
    {
        throw new NotImplementedException();
    }

    void LockCursor(bool lockCursor)
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
