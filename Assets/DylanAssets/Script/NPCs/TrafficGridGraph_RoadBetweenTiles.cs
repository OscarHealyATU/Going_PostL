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

    private Transform[,] nodes;
    private int nodeSizeX, nodeSizeZ;
    private float dist;
    private Vector3 originCorner;

    private bool ready;
    public bool IsReady => ready;

    public int NodeCount { get; private set; }
    public readonly List<Transform> allNodes = new List<Transform>();

    void Awake()
    {
        StartCoroutine(BuildWhenReady());
    }

    IEnumerator BuildWhenReady()
    {
        if (road == null) road = FindFirstObjectByType<GridifyRoadAdapter>();
        if (road == null)
        {
            Debug.LogError("TrafficGridGraph_RoadBetweenTiles: GridifyRoadAdapter not found.");
            yield break;
        }

        yield return null;

        if (!road.IsReady)
        {
            Debug.LogError("TrafficGridGraph_RoadBetweenTiles: Road adapter not ready.");
            yield break;
        }

        BuildGraph();
        ready = true;
    }

    public void BuildGraph()
    {
        if (nodeParent == null)
        {
            var go = new GameObject("TrafficNodes_RoadIntersections");
            nodeParent = go.transform;
            nodeParent.SetParent(transform);
        }

        for (int i = nodeParent.childCount - 1; i >= 0; i--)
            Destroy(nodeParent.GetChild(i).gameObject);

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

        Debug.Log($"TrafficGridGraph_RoadBetweenTiles: Built nodes={NodeCount} size=({nodeSizeX},{nodeSizeZ}) dist={dist}");
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

    public Transform[] BuildPath(Transform start, Transform goal)
    {
        if (!ready || start == null || goal == null) return null;

        Vector2Int s = WorldToNodeGrid(start.position);
        Vector2Int g = WorldToNodeGrid(goal.position);

        if (!InBounds(s) || !InBounds(g)) return null;

        var q = new Queue<Vector2Int>();
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var prevDir = new Dictionary<Vector2Int, Vector2Int>();

        q.Enqueue(s);
        prev[s] = new Vector2Int(int.MinValue, int.MinValue);
        prevDir[s] = Vector2Int.zero;

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == g) break;

            Vector2Int lastD = prevDir[cur];
            List<Vector2Int> ordered = new List<Vector2Int>(4);

            if (lastD != Vector2Int.zero && Random.value < preferStraight)
            {
                ordered.Add(lastD);
                foreach (var d in dirs) if (d != lastD) ordered.Add(d);
            }
            else
            {
                int a = Random.Range(0, 4);
                for (int i = 0; i < 4; i++) ordered.Add(dirs[(a + i) % 4]);
            }

            foreach (var d in ordered)
            {
                var nxt = cur + d * nodeEveryNIntersections;
                if (!InBounds(nxt)) continue;
                if (nodes[nxt.x, nxt.y] == null) continue;
                if (prev.ContainsKey(nxt)) continue;

                prev[nxt] = cur;
                prevDir[nxt] = d;
                q.Enqueue(nxt);
            }
        }

        if (!prev.ContainsKey(g)) return null;

        var path = new List<Transform>();
        var p = g;
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
        int ix = Mathf.RoundToInt((pos.x - originCorner.x) / dist);
        int iz = Mathf.RoundToInt((pos.z - originCorner.z) / dist);

        ix = Mathf.Clamp(ix, 0, nodeSizeX - 1);
        iz = Mathf.Clamp(iz, 0, nodeSizeZ - 1);

        // snap to our lattice step
        ix = Mathf.Clamp(ix - (ix % nodeEveryNIntersections), 0, nodeSizeX - 1);
        iz = Mathf.Clamp(iz - (iz % nodeEveryNIntersections), 0, nodeSizeZ - 1);

        return new Vector2Int(ix, iz);
    }

    bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < nodeSizeX && p.y < nodeSizeZ;
}