using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeliveryCompassUI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public string playerTag = "Player";
    public RectTransform compassStrip;
    public RectTransform deliveryMarker;
    public CanvasGroup canvasGroup;
    public TMP_Text distanceText;

    [Header("Optional")]
    public Graphic deliveryMarkerGraphic;

    [Header("Compass Strip")]
    public float pixelsPerDegree = 8f;
    public float headingOffset = 0f;

    [Header("Delivery Marker")]
    public float markerMaxOffset = 250f;
    public float markerSmoothSpeed = 10f;

    [Header("Scene Control")]
    public string mainSceneName = "Main";

    private float currentMarkerX;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            SetCompassVisible(false);
            SetMarkerVisible(false);
            SetDistanceVisible(false);
            return;
        }

        if (player == null)
        {
            SetCompassVisible(false);
            SetMarkerVisible(false);
            SetDistanceVisible(false);
            return;
        }

        SetCompassVisible(true);
        UpdateCompassStrip();
        UpdateDeliveryMarker();
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
        {
            Camera cam = go.GetComponentInChildren<Camera>();
            player = cam != null ? cam.transform : go.transform;
        }
    }

    private void UpdateCompassStrip()
    {
        if (compassStrip == null)
            return;

        float yaw = player.eulerAngles.y + headingOffset;
        float wrappedYaw = Mathf.Repeat(yaw, 360f);
        float x = wrappedYaw * pixelsPerDegree;

        Vector2 pos = compassStrip.anchoredPosition;
        pos.x = 1440f - x;
        compassStrip.anchoredPosition = pos;
    }

    private void UpdateDeliveryMarker()
    {
        if (DeliveryManager.Instance == null)
        {
            SetMarkerVisible(false);
            SetDistanceVisible(false);
            return;
        }

        Vector3? targetOpt = DeliveryManager.Instance.GetCurrentTarget();
        if (!targetOpt.HasValue)
        {
            SetMarkerVisible(false);
            SetDistanceVisible(false);
            return;
        }

        Vector3 target = targetOpt.Value;
        Vector3 toTarget = target - player.position;
        toTarget.y = 0f;

        SetMarkerVisible(true);
        SetDistanceVisible(true);

        if (toTarget.sqrMagnitude < 0.001f)
        {
            if (distanceText != null)
                distanceText.text = "0m";

            SetMarkerX(0f);
            return;
        }

        if (distanceText != null)
            distanceText.text = Mathf.RoundToInt(toTarget.magnitude) + "m";

        float signedAngle = Vector3.SignedAngle(player.forward, toTarget.normalized, Vector3.up);
        float normalized = Mathf.Clamp(signedAngle / 90f, -1f, 1f);
        float targetX = normalized * markerMaxOffset;

        currentMarkerX = Mathf.Lerp(currentMarkerX, targetX, Time.deltaTime * markerSmoothSpeed);
        SetMarkerX(currentMarkerX);
    }

    private void SetMarkerX(float x)
    {
        if (deliveryMarker == null)
            return;

        Vector2 pos = deliveryMarker.anchoredPosition;
        pos.x = x;
        deliveryMarker.anchoredPosition = pos;
    }

    private void SetCompassVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetMarkerVisible(bool visible)
    {
        if (deliveryMarker != null)
            deliveryMarker.gameObject.SetActive(visible);

        if (deliveryMarkerGraphic != null)
            deliveryMarkerGraphic.enabled = visible;
    }

    private void SetDistanceVisible(bool visible)
    {
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(visible);

            if (!visible)
                distanceText.text = string.Empty;
        }
    }
}