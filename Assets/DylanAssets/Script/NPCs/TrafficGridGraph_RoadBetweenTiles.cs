using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficGridGraph_RoadBetweenTiles : MonoBehaviour
{
    [Header("Adapter (reads gridify)")]
    public GridifyRoadAdapter road;

    [Header("Traffic Nodes (Road Intersections)")]
    public Transform nodeParent;
    public GameObject nodePrefab;

    [Tooltip("1 = every intersection, 2 = every other intersection, etc.")]
    [Range(1, 10)] public int nodeEveryNIntersections = 1;

    [Header("Path Style")]
    [Range(0f, 1f)] public float preferStraight = 0.65f;

    [Header("Node Height Offset")]
    public float nodeHeight = 0f;

    [Header("Build")]
    public float maxWaitForRoad = 10f;

    private Transform[,] nodes;
    private int nodeSizeX, nodeSizeZ;
    private float dist;
    private Vector3 originCorner;

    private bool ready;
    public bool IsReady => ready;

    public int NodeCount { get; private set; }
    public Vector3 MinBounds => originCorner;
    public Vector3 MaxBounds => originCorner + new Vector3((nodeSizeX - 1) * dist, 0f, (nodeSizeZ - 1) * dist);

    public readonly List<Transform> allNodes = new List<Transform>();

    void Awake()
    {
        StartCoroutine(BuildWhenReady());
    }

    IEnumerator BuildWhenReady()
    {
        ready = false;

        if (road == null)
            road = FindFirstObjectByType<GridifyRoadAdapter>();

        if (road == null)
        {
            //debug.LogError("TrafficGridGraph_RoadBetweenTiles: GridifyRoadAdapter not found.");
            yield break;
        }

        float elapsed = 0f;
        while (!road.IsReady && elapsed < maxWaitForRoad)
        {
            road.Refresh();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!road.IsReady)
        {
            //debug.LogError("TrafficGridGraph_RoadBetweenTiles: Road adapter never became ready.");
            yield break;
        }

        BuildGraph();
    }

    [ContextMenu("Rebuild Graph")]
    public void BuildGraph()
    {
        if (road == null)
        {
            //debug.LogError("TrafficGridGraph_RoadBetweenTiles: road adapter missing.");
            ready = false;
            return;
        }

        road.Refresh();
        if (!road.IsReady)
        {
            //debug.LogError("TrafficGridGraph_RoadBetweenTiles: road adapter is not ready.");
            ready = false;
            return;
        }

        ready = false;

        if (nodeParent == null)
        {
            GameObject go = new GameObject("TrafficNodes_RoadIntersections");
            nodeParent = go.transform;
            nodeParent.SetParent(transform);
            nodeParent.localPosition = Vector3.zero;
            nodeParent.localRotation = Quaternion.identity;
            nodeParent.localScale = Vector3.one;
        }

        for (int i = nodeParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(nodeParent.GetChild(i).gameObject);
            else
                DestroyImmediate(nodeParent.GetChild(i).gameObject);
        }

        dist = road.Dist;
        originCorner = road.OriginCorner + Vector3.up * nodeHeight;

        nodeSizeX = road.TilesX + 1;
        nodeSizeZ = road.TilesZ + 1;

        nodes = new Transform[nodeSizeX, nodeSizeZ];
        allNodes.Clear();
        NodeCount = 0;

        for (int ix = 0; ix < nodeSizeX; ix += nodeEveryNIntersections)
        {
            for (int iz = 0; iz < nodeSizeZ; iz += nodeEveryNIntersections)
            {
                Vector3 pos = originCorner + new Vector3(ix * dist, 0f, iz * dist);

                GameObject n = nodePrefab != null
                    ? Instantiate(nodePrefab, pos, Quaternion.identity, nodeParent)
                    : new GameObject($"RoadNode_{ix}_{iz}");

                n.transform.position = pos;
                n.transform.SetParent(nodeParent);

                nodes[ix, iz] = n.transform;
                allNodes.Add(n.transform);
                NodeCount++;
            }
        }

        ready = true;

    }

    public void GetNodesNear(Vector3 center, float radius, List<Transform> results)
    {
        results.Clear();
        if (!ready || allNodes.Count == 0) return;

        float r2 = radius * radius;

        for (int i = 0; i < allNodes.Count; i++)
        {
            Transform n = allNodes[i];
            if (n == null) continue;

            Vector3 d = n.position - center;
            d.y = 0f;

            if (d.sqrMagnitude <= r2)
                results.Add(n);
        }
    }

    public Transform GetClosestNode(Vector3 center)
    {
        if (!ready || allNodes.Count == 0) return null;

        Transform best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < allNodes.Count; i++)
        {
            Transform n = allNodes[i];
            if (n == null) continue;

            Vector3 d = n.position - center;
            d.y = 0f;

            float sqr = d.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = n;
            }
        }

        return best;
    }

    public bool ContainsWorldXZ(Vector3 pos)
    {
        if (!ready) return false;

        return pos.x >= MinBounds.x && pos.x <= MaxBounds.x &&
               pos.z >= MinBounds.z && pos.z <= MaxBounds.z;
    }

    public Transform[] BuildPath(Transform start, Transform goal)
    {
        if (!ready || start == null || goal == null) return null;

        Vector2Int s = WorldToNodeGrid(start.position);
        Vector2Int g = WorldToNodeGrid(goal.position);

        if (!InBounds(s) || !InBounds(g)) return null;
        if (nodes[s.x, s.y] == null || nodes[g.x, g.y] == null) return null;

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> prev = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> prevDir = new Dictionary<Vector2Int, Vector2Int>();

        q.Enqueue(s);
        prev[s] = new Vector2Int(int.MinValue, int.MinValue);
        prevDir[s] = Vector2Int.zero;

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            if (cur == g) break;

            Vector2Int lastD = prevDir[cur];
            List<Vector2Int> ordered = new List<Vector2Int>(4);

            if (lastD != Vector2Int.zero && Random.value < preferStraight)
            {
                ordered.Add(lastD);
                foreach (Vector2Int d in dirs)
                    if (d != lastD) ordered.Add(d);
            }
            else
            {
                int a = Random.Range(0, 4);
                for (int i = 0; i < 4; i++)
                    ordered.Add(dirs[(a + i) % 4]);
            }

            foreach (Vector2Int d in ordered)
            {
                Vector2Int nxt = cur + d * nodeEveryNIntersections;

                if (!InBounds(nxt)) continue;
                if (nodes[nxt.x, nxt.y] == null) continue;
                if (prev.ContainsKey(nxt)) continue;

                prev[nxt] = cur;
                prevDir[nxt] = d;
                q.Enqueue(nxt);
            }
        }

        if (!prev.ContainsKey(g)) return null;

        List<Transform> path = new List<Transform>();
        Vector2Int p = g;

        while (p.x != int.MinValue)
        {
            path.Add(nodes[p.x, p.y]);
            p = prev[p];
        }

        path.Reverse();
        return path.ToArray();
    }

    Vector2Int WorldToNodeGrid(Vector3 pos)
    {
        if (dist <= 0f)
            return Vector2Int.zero;

        int ix = Mathf.RoundToInt((pos.x - originCorner.x) / dist);
        int iz = Mathf.RoundToInt((pos.z - originCorner.z) / dist);

        ix = Mathf.Clamp(ix, 0, nodeSizeX - 1);
        iz = Mathf.Clamp(iz, 0, nodeSizeZ - 1);

        ix = Mathf.Clamp(ix - (ix % nodeEveryNIntersections), 0, nodeSizeX - 1);
        iz = Mathf.Clamp(iz - (iz % nodeEveryNIntersections), 0, nodeSizeZ - 1);

        return new Vector2Int(ix, iz);
    }

    bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.y >= 0 && p.x < nodeSizeX && p.y < nodeSizeZ;
    }
}