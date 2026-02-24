using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSavedPoint : MonoBehaviour
{
    private const string MAIN_SCENE_NAME = "Main";

    void Start()
    {
        if (SceneManager.GetActiveScene().name != MAIN_SCENE_NAME)
            return;

        if (PlayerService.TryGetReturnPoint(out Vector3 position, out float yaw))
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            PlayerService.ClearReturnPoint();

            Debug.Log($"📍 Restored player return position in Main: {position} yaw={yaw}");
        }
    }
}