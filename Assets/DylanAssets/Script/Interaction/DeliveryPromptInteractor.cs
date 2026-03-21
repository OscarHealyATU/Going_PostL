using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class DeliveryPromptInteractor : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public string playerTag = "Player";
    public TMP_Text promptText;

    [Header("Scene Control")]
    public string mainSceneName = "Main";

    [Header("Delivery")]
    public float interactRadius = 4f;
    public KeyCode deliverKey = KeyCode.E;

    private Vector3 currentTarget;
    private bool canDeliver;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TryFindPlayer();
        SetPromptVisible(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        TryFindPlayer();
        SetPromptVisible(false);
    }

    private void Update()
    {
        TryFindPlayer();

        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            canDeliver = false;
            SetPromptVisible(false);
            return;
        }

        if (player == null || DeliveryManager.Instance == null)
        {
            canDeliver = false;
            SetPromptVisible(false);
            return;
        }

        Vector3? targetOpt = DeliveryManager.Instance.GetCurrentTarget();
        if (!targetOpt.HasValue)
        {
            canDeliver = false;
            SetPromptVisible(false);
            return;
        }

        currentTarget = targetOpt.Value;

        Vector3 flatPlayer = player.position;
        Vector3 flatTarget = currentTarget;
        flatPlayer.y = 0f;
        flatTarget.y = 0f;

        float dist = Vector3.Distance(flatPlayer, flatTarget);
        canDeliver = dist <= interactRadius;

        SetPromptVisible(canDeliver);

        if (canDeliver && Input.GetKeyDown(deliverKey))
        {
            TryCompleteDelivery();
        }
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            player = go.transform;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText == null)
            return;

        promptText.gameObject.SetActive(visible);

        if (visible)
            promptText.text = "Press E to deliver";
        else
            promptText.text = string.Empty;
    }

    private void TryCompleteDelivery()
    {
        if (DeliveryManager.Instance == null)
            return;

        // Use whichever method name matches your DeliveryManager.
        // Pick ONE of these patterns and remove the others.

        // Option A:
        // DeliveryManager.Instance.CompleteCurrentDelivery();

        // Option B:
        // DeliveryManager.Instance.DeliverCurrentPackage();

        // Option C:
        // DeliveryManager.Instance.TryDeliverAtPlayerPosition();

        // Temporary fallback log so you know the input works.
        Debug.Log("E pressed in delivery radius. Hook this into your DeliveryManager completion method.");
    }
}