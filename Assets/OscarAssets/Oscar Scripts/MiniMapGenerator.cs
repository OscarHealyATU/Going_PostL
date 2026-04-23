using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapGenerator : MonoBehaviour
{
    public Camera miniMapCamera;
    public RawImage miniMapDisplay;
    public int mapWidth = 4000, mapHeight = 4800;

    public PlayerMovementOutside playerMovement;
    public PlayerLook playerLook;

    public float mapMinX, mapMaxX, mapMinZ, mapMaxZ;

    IEnumerator Start()
    {
        // Sanity checks
        if (miniMapCamera == null) { Debug.LogError("[MiniMap] miniMapCamera not assigned!"); yield break; }
        if (miniMapDisplay == null) { Debug.LogError("[MiniMap] miniMapDisplay not assigned!"); yield break; }

        Debug.Log($"[MiniMap] Camera pos: {miniMapCamera.transform.position}, ortho: {miniMapCamera.orthographic}, size: {miniMapCamera.orthographicSize}, culling mask: {miniMapCamera.cullingMask}");

        if (playerMovement != null) playerMovement.canMove = false;
        if (playerLook != null) playerLook.canLook = false;

        // Give gridify a couple of frames to finish instantiating
        yield return null;
        yield return new WaitForEndOfFrame();

        CaptureMap();

        miniMapCamera.enabled = false;

        if (playerMovement != null) playerMovement.canMove = true;
        if (playerLook != null) playerLook.canLook = true;
    }

    void CaptureMap()
    {
        float orthographicSize = miniMapCamera.orthographicSize;
        float aspect = (float)mapWidth / mapHeight;
        float halfWidth = orthographicSize * aspect;
        float halfHeight = orthographicSize;

        Vector3 camPos = miniMapCamera.transform.position;
        mapMinX = camPos.x - halfWidth;
        mapMaxX = camPos.x + halfWidth;
        mapMinZ = camPos.z - halfHeight;
        mapMaxZ = camPos.z + halfHeight;

        Debug.Log($"[MiniMap] Bounds: X({mapMinX} to {mapMaxX}), Z({mapMinZ} to {mapMaxZ})");

        RenderTexture rt = new RenderTexture(mapWidth, mapHeight, 24);
        miniMapCamera.targetTexture = rt;
        miniMapCamera.Render();

        RenderTexture.active = rt;
        Texture2D miniMapTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
        miniMapTexture.ReadPixels(new Rect(0, 0, mapWidth, mapHeight), 0, 0);
        miniMapTexture.Apply();
        RenderTexture.active = null;

        if (miniMapDisplay != null)
        {
            miniMapDisplay.texture = miniMapTexture;
            Debug.Log($"[MiniMap] Texture assigned. Size: {miniMapTexture.width}x{miniMapTexture.height}");
        }

        miniMapCamera.targetTexture = null;
        rt.Release();
        Destroy(rt);
    }
}