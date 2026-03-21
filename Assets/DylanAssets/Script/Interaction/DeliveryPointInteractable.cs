using UnityEngine;

public class DeliveryPointInteractable : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float promptRadius = 3f;

    [Header("Optional")]
    public GameObject promptObject;
    public Transform visualRoot;

    [Header("Animation")]
    public bool bob = true;
    public float bobAmount = 0.35f;
    public float bobSpeed = 2.5f;
    public bool rotate = true;
    public float rotateSpeed = 90f;

    private DeliveryJob boundJob;
    private Transform player;
    private float interactRadius = 10f;
    private string playerTag = "Player";

    private Vector3 visualStartLocalPos;

    public void Initialize(DeliveryJob job, Transform playerTransform, float radius, string playerTagValue)
    {
        boundJob = job;
        player = playerTransform;
        interactRadius = radius;
        playerTag = playerTagValue;

        if (visualRoot != null)
            visualStartLocalPos = visualRoot.localPosition;

        SetPromptVisible(false);
    }

    private void Start()
    {
        if (visualRoot != null)
            visualStartLocalPos = visualRoot.localPosition;

        SetPromptVisible(false);
    }

    private void Update()
    {
        AnimateVisual();
        TryFindPlayer();

        if (boundJob == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (player == null)
        {
            SetPromptVisible(false);
            return;
        }

        Vector3 flatPlayer = player.position;
        Vector3 flatTarget = transform.position;
        flatPlayer.y = 0f;
        flatTarget.y = 0f;

        float dist = Vector3.Distance(flatPlayer, flatTarget);
        bool inInteractRange = dist <= interactRadius;
        bool inPromptRange = dist <= promptRadius;

        SetPromptVisible(inPromptRange);

        if (inInteractRange && Input.GetKeyDown(interactKey))
        {
            if (DeliveryManager.Instance != null)
                DeliveryManager.Instance.TryCompleteDeliveryFromPoint(this, boundJob);
        }
    }

    private void AnimateVisual()
    {
        if (visualRoot == null)
            return;

        Vector3 localPos = visualStartLocalPos;

        if (bob)
            localPos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        visualRoot.localPosition = localPos;

        if (rotate)
            visualRoot.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            player = go.transform;
    }

    public DeliveryJob GetBoundJob()
    {
        return boundJob;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptObject != null)
            promptObject.SetActive(visible);
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
    }
}