using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsBtn : MonoBehaviour
{
     [SerializeField] private string sceneName = "Settings";

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
