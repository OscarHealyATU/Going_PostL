using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchSceneTrigger : MonoBehaviour
{
    [SerializeField] private BoxCollider triggerCollider;
    [SerializeField] private string sceneName = "Warehouse";

    private bool isLoading = false;

    private void Start()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponents<BoxCollider>()
                .FirstOrDefault(bc => bc.isTrigger);
        }

        if (triggerCollider == null)
        {
            //debug.LogError($"❌ SwitchSceneTrigger on '{gameObject.name}' could not find a trigger BoxCollider.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;

        LoadScene();
    }

    private void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            //debug.LogError($"❌ SwitchSceneTrigger on '{gameObject.name}' has no scene name assigned.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            //debug.LogError($"❌ Scene '{sceneName}' cannot be loaded. Check Build Settings.");
            return;
        }

        isLoading = true;

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}