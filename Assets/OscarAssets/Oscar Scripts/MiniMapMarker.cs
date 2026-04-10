using UnityEngine;
using UnityEngine.UI;

public class MiniMapMarker : MonoBehaviour
{
    public Transform Player;
    public MiniMapGenerator miniMap;
    public RectTransform minimapImage, playerMarker;
    public RawImage miniMapDisplay;

    public float zoomPercent = 0.3f;

    void Update()
    {
        float normX = Mathf.InverseLerp(miniMap.mapMinX, miniMap.mapMaxX, Player.position.x);
        float normZ = Mathf.InverseLerp(miniMap.mapMinZ, miniMap.mapMaxZ, Player.position.z);

        float halfZoom = zoomPercent / 2f;
        float uvX = Mathf.Clamp(normX - halfZoom, 0f, 1f - zoomPercent);
        float uvZ = Mathf.Clamp(normZ - halfZoom, 0f, 1f - zoomPercent);

        miniMapDisplay.uvRect = new Rect(uvX, uvZ, zoomPercent, zoomPercent);

        RectTransform mapRect = miniMapDisplay.rectTransform;
        float markerNormX = (normX - uvX) / zoomPercent;
        float markerNormZ = (normZ - uvZ) / zoomPercent;

        playerMarker.anchoredPosition = new Vector2(
            markerNormX * minimapImage.rect.width,
            markerNormZ * minimapImage.rect.height
        );

        playerMarker.localEulerAngles = new Vector3(0, 0, -Player.eulerAngles.y);
    }
}
