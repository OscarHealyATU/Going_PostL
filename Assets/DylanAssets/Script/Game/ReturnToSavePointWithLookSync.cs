using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class ReturnToSavedPointWithLookSync : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainSceneName = "Main";

    [Header("Rotation")]
    [SerializeField] private bool rotate180OnReturn = true;
    [SerializeField] private float restoredCameraPitch = 0f;

    [Header("Options")]
    [SerializeField] private bool clearReturnPointAfterUse = true;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != mainSceneName)
            return;

        if (DbBoot.Instance == null)
            return;

        if (!PlayerService.TryGetReturnPoint(out Vector3 savedPosition, out float savedYaw))
            return;

        float restoredYaw = rotate180OnReturn ? savedYaw + 180f : savedYaw;

        CharacterController controller = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();
        PlayerMovementOutside movement = GetComponent<PlayerMovementOutside>();
        PlayerLookOutside look = GetComponent<PlayerLookOutside>();

        if (movement != null)
            movement.canMove = false;

        if (controller != null)
            controller.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        transform.position = savedPosition;

        if (look != null)
            look.ForceLook(restoredYaw, restoredCameraPitch);
        else
            transform.rotation = Quaternion.Euler(0f, restoredYaw, 0f);

        if (controller != null)
            controller.enabled = true;

        if (rb != null)
            rb.isKinematic = false;

        if (movement != null)
            movement.canMove = true;

        if (clearReturnPointAfterUse)
            PlayerService.ClearReturnPoint();

        Debug.Log($"[ReturnPoint] Restored player to {savedPosition} with yaw {restoredYaw}");
    }
}