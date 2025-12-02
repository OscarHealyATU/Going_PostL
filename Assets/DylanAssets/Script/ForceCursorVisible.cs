using UnityEngine;

public class ForceCursorVisible : MonoBehaviour
{
    void OnEnable()
    {
        // Ensure cursor is always unlocked + visible when entering this scene
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Constantly override anything trying to hide or lock the cursor
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;
    }

    void OnDisable()
{
    // Restore typical Unity FPS behavior or your default
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

}
