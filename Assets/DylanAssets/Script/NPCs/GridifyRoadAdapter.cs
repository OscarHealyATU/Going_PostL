using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Adapter around gridify.
/// Treats gridify's spawned positions as LOT centers.
/// Roads are BETWEEN lots, so road intersections live on CORNERS between tile centers.
/// Includes editor-time gizmo preview.
/// </summary>
[ExecuteAlways]
public class GridifyRoadAdapter : MonoBehaviour
{
    [Header("Source")]
    public gridify city;

    [Header("Road Height")]
    [Tooltip("World Y where cars should drive (doesn't affect gridify).")]
    public float roadY = 0f;

    [Header("Editor Preview")]
    public bool showPreview = true;
    public bool drawLotCenters = true;
    public bool drawIntersections = true;
    public bool drawRoadLines = true;

    [Tooltip("Preview spacing size for lot center markers (in world units).")]
    public float lotMarkerSize = 1.0f;

    [Tooltip("Preview spacing size for intersection markers (in world units).")]
    public float intersectionMarkerSize = 0.8f;

    [Tooltip("How many tiles to draw in the editor (0 = full grid). Useful for huge grids.")]
    public int previewLimit = 0;

    public int TilesX { get; private set; }
    public int TilesZ { get; private set; }
    public float Dist { get; private set; }
    public Vector3 OriginCorner { get; private set; }
    public Vector3 MaxCorner { get; private set; }
    public bool IsReady { get; private set; }

    void OnEnable()
    {
        Refresh();
    }

    void Awake()
    {
        Refresh();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Refresh();
#endif
    }

    public void Refresh()
    {
        if (city == null)
            city = FindFirstObjectByType<gridify>();

        if (city == null)
        {
            IsReady = false;
            return;
        }

        TilesX = Mathf.Max(0, Mathf.FloorToInt(city.noOfHousesX));
        TilesZ = Mathf.Max(0, Mathf.FloorToInt(city.noOfHousesZ));
        Dist = city.distance;

        if (Dist <= 0f || TilesX <= 0 || TilesZ <= 0)
        {
            IsReady = false;
            return;
        }

        // gridify places LOT CENTERS at:
        // xStart + x * distance
        // zStart + z * distance
        //
        // road intersections are half a tile outward from the first lot center
        OriginCorner = new Vector3(
            city.xStartPosition - Dist * 0.5f,
            roadY,
            city.zStartPosition - Dist * 0.5f
        );

        MaxCorner = OriginCorner + new Vector3(TilesX * Dist, 0f, TilesZ * Dist);

        IsReady = true;
    }

    /// <summary>
    /// Intersections are on a lattice: (TilesX+1) by (TilesZ+1)
    /// ix in [0..TilesX], iz in [0..TilesZ]
    /// </summary>
    public Vector3 IntersectionWorld(int ix, int iz)
    {
        return OriginCorner + new Vector3(ix * Dist, 0f, iz * Dist);
    }

    public Vector2Int WorldToIntersection(Vector3 pos)
    {
        if (!IsReady || Dist <= 0f)
            return Vector2Int.zero;

        float fx = (pos.x - OriginCorner.x) / Dist;
        float fz = (pos.z - OriginCorner.z) / Dist;

        int ix = Mathf.RoundToInt(fx);
        int iz = Mathf.RoundToInt(fz);

        ix = Mathf.Clamp(ix, 0, TilesX);
        iz = Mathf.Clamp(iz, 0, TilesZ);

        return new Vector2Int(ix, iz);
    }

    public bool ContainsWorldXZ(Vector3 pos)
    {
        if (!IsReady) return false;

        return pos.x >= OriginCorner.x && pos.x <= MaxCorner.x &&
               pos.z >= OriginCorner.z && pos.z <= MaxCorner.z;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showPreview) return;

        if (!Application.isPlaying)
            Refresh();

        if (!IsReady || city == null) return;

        int sx = TilesX;
        int sz = TilesZ;

        if (previewLimit > 0)
        {
            sx = Mathf.Min(sx, previewLimit);
            sz = Mathf.Min(sz, previewLimit);
        }

        float y = roadY;

        if (drawLotCenters)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            for (int x = 0; x < sx; x++)
            {
                for (int z = 0; z < sz; z++)
                {
                    Vector3 lotCenter = new Vector3(
                        city.xStartPosition + x * Dist,
                        y + 0.02f,
                        city.zStartPosition + z * Dist
                    );

                    Gizmos.DrawCube(lotCenter, new Vector3(lotMarkerSize, 0.05f, lotMarkerSize));
                }
            }
        }

        if (drawRoadLines)
        {
            Gizmos.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

            for (int ix = 0; ix <= sx; ix++)
            {
                float wx = OriginCorner.x + ix * Dist;
                Vector3 a = new Vector3(wx, y + 0.06f, OriginCorner.z);
                Vector3 b = new Vector3(wx, y + 0.06f, OriginCorner.z + sz * Dist);
                Gizmos.DrawLine(a, b);
            }

            for (int iz = 0; iz <= sz; iz++)
            {
                float wz = OriginCorner.z + iz * Dist;
                Vector3 a = new Vector3(OriginCorner.x, y + 0.06f, wz);
                Vector3 b = new Vector3(OriginCorner.x + sx * Dist, y + 0.06f, wz);
                Gizmos.DrawLine(a, b);
            }
        }

        if (drawIntersections)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.85f);
            for (int ix = 0; ix <= sx; ix++)
            {
                for (int iz = 0; iz <= sz; iz++)
                {
                    Vector3 p = IntersectionWorld(ix, iz);
                    p.y = y + 0.10f;
                    Gizmos.DrawSphere(p, intersectionMarkerSize * 0.25f);
                }
            }
        }

        Handles.color = new Color(1f, 1f, 1f, 0.8f);
        Handles.Label(
            OriginCorner + Vector3.up * (y + 0.2f),
            $"GridifyRoadAdapter Preview\nTiles: {TilesX}x{TilesZ}  Dist: {Dist}\nOriginCorner: {OriginCorner}\nMaxCorner: {MaxCorner}"
        );
    }
#endif
}