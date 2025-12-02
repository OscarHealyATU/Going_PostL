using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchSceneTrigger : MonoBehaviour
{
    [SerializeField] private BoxCollider triggerCollider;
    [SerializeField] private string sceneName = "Warehouse";

    private void Start()
    {
        // If you didn't assign it in Inspector, find the trigger collider
        if (triggerCollider == null)
        {
            triggerCollider = GetComponents<BoxCollider>()
                .First(bc => bc.isTrigger);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that triggered is the player
        if (other.CompareTag("Player"))
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        // Load by scene name
        SceneManager.LoadScene(sceneName);


        
        // Or load by scene index:
         SceneManager.LoadScene(sceneName);
    }
}