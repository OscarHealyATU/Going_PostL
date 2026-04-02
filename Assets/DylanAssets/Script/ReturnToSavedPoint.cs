using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSavedPoint : MonoBehaviour
{
    private const string MAIN_SCENE_NAME = "Main";

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != MAIN_SCENE_NAME)
            yield break;

        // Let all spawn / movement systems finish first
        yield return null;
        yield return null;

        if (!PlayerService.TryGetReturnPoint(out Vector3 position, out float yaw))
        {
            Debug.Log("[ReturnPoint] No saved return point found.");
            yield break;
        }

        Debug.Log("[ReturnPoint] Applying saved return point...");

        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (cc != null)
            cc.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw + 180f, 0f);

        yield return null;

        if (cc != null)
            cc.enabled = true;

        if (rb != null)
            rb.isKinematic = false;

        PlayerService.ClearReturnPoint();

        Debug.Log($"[ReturnPoint] Restored player to {position} with yaw {yaw + 180f}");
    }
}