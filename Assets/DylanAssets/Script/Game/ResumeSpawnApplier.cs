using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResumeSpawnApplier : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float delay = 0.1f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delay);

        if (!ResumeSpawnData.HasPendingSpawn)
            yield break;

        string currentScene = SceneManager.GetActiveScene().name;
        if (ResumeSpawnData.SceneName != currentScene)
            yield break;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[ResumeSpawnApplier] No player found with tag: " + playerTag);
            yield break;
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.transform.position = ResumeSpawnData.Position;
        player.transform.rotation = Quaternion.Euler(0f, ResumeSpawnData.Yaw, 0f);

        if (controller != null)
            controller.enabled = true;

        ResumeSpawnData.HasPendingSpawn = false;
    }
}