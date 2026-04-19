using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Vehicles")]
    public GameObject[] carPrefabs;

    [Header("Routing")]
    public TrafficGridGraph_RoadBetweenTiles graph;

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Spawn Range (around player)")]
    public float startNodeRadius = 100f;
    public float endNodeRadius = 200f;

    [Header("Spawn Timing")]
    public float spawnInterval = 0.3f;
    public float spawnJitter = 0.75f;
    public int maxAliveTotal = 40;

    [Header("Trip Length")]
    public int minWaypoints = 20;
    public float minEndDistanceWorld = 120f;

    [Header("Driving Style")]
    public bool driveOnRight = false;
    [Range(0f, 0.5f)] public float speedJitter = 0.15f;

    [Header("Lane")]
    public float laneWidth = 0.7f;
    public float laneJitter = 0.15f;

    [Header("Cleanup")]
    public bool despawnFarCars = true;
    public float despawnRadius = 450f;

    [Header("Perf")]
    public float nearCacheRefresh = 0.5f;

    private float timer;
    private float nextSpawnTime;
    private int aliveCount;

    private float nearCacheTimer = 999f;
    private bool hasBuiltNearCache = false;

    private readonly List<Transform> nearStartNodes = new List<Transform>(512);
    private readonly List<Transform> nearEndNodes = new List<Transform>(512);

    private bool warnedNoPlayer, warnedNoGraph, warnedNoNodes;

    void Start()
    {
        ResolvePlayer();
        ScheduleNextSpawn();
    }

    void ResolvePlayer()
    {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    void Update()
    {
        if (player == null) ResolvePlayer();
        if (player == null)
        {
            if (!warnedNoPlayer)
            {
                Debug.LogWarning("CarSpawner: player not found. Assign 'player' or set correct Player tag.");
                warnedNoPlayer = true;
            }
            return;
        }

        if (graph == null || !graph.IsReady)
        {
            if (!warnedNoGraph)
            {
                Debug.LogWarning("CarSpawner: graph missing or not ready yet.");
                warnedNoGraph = true;
            }
            return;
        }

        nearCacheTimer += Time.deltaTime;
        if (nearCacheTimer >= nearCacheRefresh)
        {
            nearCacheTimer = 0f;

            graph.GetNodesNear(player.position, startNodeRadius, nearStartNodes);
            graph.GetNodesNear(player.position, endNodeRadius, nearEndNodes);
            hasBuiltNearCache = true;

            Transform closest = graph.GetClosestNode(player.position);

            if (closest != null)
            {
                Vector3 a = player.position;
                Vector3 b = closest.position;
                a.y = 0f;
                b.y = 0f;

                float closestDist = Vector3.Distance(a, b);

                // Debug.Log(
                //     $"[CarSpawner] player={player.position} " +
                //     $"graphMin={graph.MinBounds} graphMax={graph.MaxBounds} " +
                //     $"insideGraph={graph.ContainsWorldXZ(player.position)} " +
                //     $"startNear={nearStartNodes.Count} endNear={nearEndNodes.Count} " +
                //     $"closestNode={closest.position} closestDist={closestDist}"
                // );
            }
            else
            {
                Debug.LogWarning("[CarSpawner] Graph has no nodes.");
            }

            if (!warnedNoNodes && (nearStartNodes.Count == 0 || nearEndNodes.Count == 0))
            {
                Debug.LogWarning($"CarSpawner: near node lists empty. startNear={nearStartNodes.Count} endNear={nearEndNodes.Count}.");
                warnedNoNodes = true;
            }
        }

        timer += Time.deltaTime;
        if (timer >= nextSpawnTime)
        {
            TrySpawnCarNearPlayerFast();
            ScheduleNextSpawn();
        }
    }

    void ScheduleNextSpawn()
    {
        timer = 0f;
        nextSpawnTime = Mathf.Max(0.05f, spawnInterval + Random.Range(-spawnJitter, spawnJitter));
    }

    static float DistanceXZ(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    void TrySpawnCarNearPlayerFast()
    {
        if (carPrefabs == null || carPrefabs.Length == 0) return;
        if (aliveCount >= maxAliveTotal) return;

        if (!hasBuiltNearCache) return;
        if (nearStartNodes.Count == 0 || nearEndNodes.Count == 0) return;

        Transform start = nearStartNodes[Random.Range(0, nearStartNodes.Count)];
        if (start == null) return;

        Transform[] path = null;
        for (int tries = 0; tries < 40; tries++)
        {
            Transform end = nearEndNodes[Random.Range(0, nearEndNodes.Count)];
            if (end == null || end == start) continue;

            if (DistanceXZ(start.position, end.position) < minEndDistanceWorld)
                continue;

            path = graph.BuildPath(start, end);
            if (path == null || path.Length < minWaypoints) continue;

            break;
        }

        if (path == null || path.Length < 2) return;

        Vector3 spawnPos = path[0].position;

        // face toward next waypoint
        Vector3 fwd = path[1].position - path[0].position;
        fwd.y = 0f;
        Quaternion spawnRot = fwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(fwd) : Quaternion.identity;

        var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        var car = Instantiate(prefab, spawnPos, spawnRot);

        var follow = car.GetComponent<WayPointFollow>();
        if (follow == null)
        {
            Destroy(car);
            return;
        }

        var wp = new GameObject[path.Length];
        for (int i = 0; i < path.Length; i++) wp[i] = path[i].gameObject;
        follow.SetWaypoints(wp);

        follow.SetDriveOnRight(driveOnRight);
        follow.SetLane(laneWidth, laneJitter);
        follow.SetSpeedMultiplier(1f + Random.Range(-speedJitter, speedJitter));

        if (despawnFarCars)
            follow.SetDespawnDistance(player, despawnRadius);

        aliveCount++;
        follow.onDespawned += () => aliveCount--;
    }
}