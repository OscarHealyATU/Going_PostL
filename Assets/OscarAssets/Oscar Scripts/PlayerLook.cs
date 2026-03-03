using UnityEngine;

public class PlayerLook : MonoBehaviour
{
   private float mouseSensitivity = 100f;
   private float lookSmoothing = 12f;
   [Header("Player Camera")]
   public Transform cameraRoot;

    private float xRotation = 0f;   
    private float curXRotation = 0f;
    private float curYRotation = 0f;
    private float targetYRotation = 0f;
    [HideInInspector]   public bool canLook = true;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        targetYRotation = transform.eulerAngles.y;
    }

 
    void Update()
    {
        if (!canLook) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);
        targetYRotation += mouseX;
        // lerp smooths out player rotation, with the aim of making it feel more consitent ~ Oscar 
        curXRotation = Mathf.Lerp(curXRotation, xRotation, lookSmoothing * Time.deltaTime);
        curYRotation = Mathf.Lerp(curYRotation, targetYRotation, lookSmoothing * Time.deltaTime);

        cameraRoot.localRotation = Quaternion.Euler(curXRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, curYRotation, 0f);
    }
}
