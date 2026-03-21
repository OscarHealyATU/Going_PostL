using UnityEngine;
using UnityEngine.SceneManagement;

public class DeliveryWorldMarker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public string playerTag = "Player";
    public Transform visualRoot;
    public Camera targetCamera;

    [Header("Scene Control")]
    public string mainSceneName = "Main";

    [Header("Positioning")]
    public float heightOffset = 4f;
    public float hideDistance = 2f;

    [Header("Animation")]
    public bool bob = true;
    public float bobAmount = 0.35f;
    public float bobSpeed = 2.5f;
    public bool rotate = true;
    public float rotateSpeed = 90f;

    [Header("Billboard")]
    public bool faceCamera = false;

    private Vector3 currentTarget;
    private bool hasTarget;

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
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        TryFindPlayer();
    }

    private void Update()
    {
        TryFindPlayer();

        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            SetVisible(false);
            return;
        }

        if (player == null)
        {
            SetVisible(false);
            return;
        }

        if (DeliveryManager.Instance == null)
        {
            SetVisible(false);
            return;
        }

        Vector3? targetOpt = DeliveryManager.Instance.GetCurrentTarget();
        if (!targetOpt.HasValue)
        {
            SetVisible(false);
            return;
        }

        currentTarget = targetOpt.Value;

        Vector3 flatPlayer = player.position;
        Vector3 flatTarget = currentTarget;
        flatPlayer.y = 0f;
        flatTarget.y = 0f;

        float distance = Vector3.Distance(flatPlayer, flatTarget);
        if (distance <= hideDistance)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdatePosition();
        UpdateRotation();
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            player = go.transform;
    }

    private void UpdatePosition()
    {
        Vector3 pos = currentTarget;
        pos.y += heightOffset;

        if (bob)
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        transform.position = pos;
    }

    private void UpdateRotation()
    {
        if (faceCamera && targetCamera != null)
        {
            Vector3 dir = transform.position - targetCamera.transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
        else if (rotate)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }
    }

    private void SetVisible(bool visible)
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }
}