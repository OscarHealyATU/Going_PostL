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
    

    // Update is called once per frame
    IEnumerator Start()
    {
        if(playerMovement != null) playerMovement.canMove = false;
        if(playerLook != null) playerLook.canLook = false;

        yield return null;

        CaptureMap();

        miniMapCamera.enabled = false;

        if(playerMovement != null) playerMovement.canMove = true;
        if(playerLook != null) playerLook.canLook = true;
    }

    void CaptureMap()
    {
        float orthographicSize = miniMapCamera.orthographicSize;
        float aspect = (float) mapWidth / mapHeight;
        float halfWidth = orthographicSize * aspect;
        float halfHeight = orthographicSize;

        Vector3 camPos = miniMapCamera.transform.position;
        mapMinX = camPos.x - halfWidth;
        mapMaxX = camPos.x + halfWidth;
        mapMinZ = camPos.z - halfHeight;
        mapMaxZ = camPos.z + halfHeight;

        RenderTexture rt = new RenderTexture(mapWidth,mapHeight,24);
        miniMapCamera.targetTexture = rt;
        miniMapCamera.Render();

        RenderTexture.active = rt;
        Texture2D miniMapTexture = new Texture2D(
            mapWidth, 
            mapHeight,
            TextureFormat.RGB24, 
            false
            );
        miniMapTexture.ReadPixels(
            new Rect(0,0,mapWidth,mapHeight)
            ,0,0);
        miniMapTexture.Apply();
        RenderTexture.active = null;

        if(miniMapDisplay != null) miniMapDisplay.texture = miniMapTexture;
        
        miniMapCamera.targetTexture = null;
        rt.Release();
        Destroy(rt);
    }
}
