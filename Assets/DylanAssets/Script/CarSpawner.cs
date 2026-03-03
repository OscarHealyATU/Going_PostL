using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Vehicles")]
    public GameObject[] carPrefabs;

    [Header("Routing")]
    public TrafficGridGraph graph;

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Spawn Range (around player)")]
    public float startNodeRadius = 150f;
    public float endNodeRadius = 600f;

    [Header("Spawn Timing")]
    public float spawnInterval = 0.1f;
    public float spawnJitter = 0.75f;
    public float minSpawnDistance = 8f;
    public int maxAliveTotal = 50; // total from this spawner/manager

    [Header("Trip Length")]
    public int minWaypoints = 10;
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

    // force refresh on first Update
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

        // Refresh near lists periodically (XZ handled inside graph.GetNodesNear)
        nearCacheTimer += Time.deltaTime;
        if (nearCacheTimer >= nearCacheRefresh)
        {
            nearCacheTimer = 0f;

            graph.GetNodesNear(player.position, startNodeRadius, nearStartNodes);
            graph.GetNodesNear(player.position, endNodeRadius, nearEndNodes);
            hasBuiltNearCache = true;

            if (!warnedNoNodes && (nearStartNodes.Count == 0 || nearEndNodes.Count == 0))
            {
                Debug.LogWarning(
                    $"CarSpawner: near node lists empty. startNear={nearStartNodes.Count} endNear={nearEndNodes.Count}. " +
                    $"Try increasing radii or check graph bounds/logs.");
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

        // Pick start near player
        Transform start = nearStartNodes[Random.Range(0, nearStartNodes.Count)];
        if (start == null) return;

        // Pick end near player-ish + build a valid path
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

        // spacing check: avoid piling cars on the exact same node
        // (simple scan over a few nearby cars would be better, but this is cheap)
        // If you want this stronger, keep a small list of last N spawned positions.
        // For now, just prevent spawns too close to player position (optional).
        // if (DistanceXZ(spawnPos, player.position) < 5f) return;

        // Spawn
        var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        var car = Instantiate(prefab, spawnPos, path[0].rotation);

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