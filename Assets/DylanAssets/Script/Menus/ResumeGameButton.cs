using UnityEngine;
using UnityEngine.SceneManagement;

public class ResumeGameButton : MonoBehaviour
{
    [SerializeField] private string fallbackScene = "Main";
    //[SerializeField] private string playerTag = "Player";

    public void ResumeGame()
    {
        if (PlayerService.TryGetResumePoint(out string sceneName, out Vector3 position, out float yaw))
        {
            ResumeSpawnData.SceneName = sceneName;
            ResumeSpawnData.Position = position;
            ResumeSpawnData.Yaw = yaw;
            ResumeSpawnData.HasPendingSpawn = true;

            SceneManager.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(fallbackScene);
        }
    }
}