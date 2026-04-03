using System;
using UnityEngine;

/// <summary>
/// Drives a vehicle along a waypoint list (road intersection nodes).
/// Designed for "roads are between tiles" where waypoints are on intersections.
/// - Lane offset is applied perpendicular to travel direction (grid-friendly).
/// - Arrive check is XZ-only (ignores height differences).
/// - Optional despawn when far from player for performance.
/// </summary>
public class WayPointFollow : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private GameObject[] waypoints;

    [Header("Movement")]
    public float speed = 20.0f;
    public float rotSpeed = 5.0f;
    public float lookAhead = 10.0f;

    [Header("Arrive Tuning")]
    public float arriveDistance = 3f;
    public float minArriveDistance = 0.2f;

    [Header("Lane")]
    [Tooltip("Half-lane offset from road centerline.")]
    [SerializeField] private float laneWidth = 0.5f;

    [Tooltip("Random per-car lane jitter added on top of laneWidth.")]
    [SerializeField] private float laneJitter = 0.25f;

    [Tooltip("+1 = right, -1 = left (relative to travel direction).")]
    private float laneSideSign = +1f;

    private float laneJitterValue;
    private float speedMult = 1f;

    private int currentWP = 0;
    private GameObject tracker;

    public Action onDespawned;

    // --- Optional performance cleanup ---
    private Transform despawnPlayer;
    private float despawnRadiusSqr = -1f;

    // --- Public API ---
    public void SetWaypoints(GameObject[] wp)
    {
        waypoints = wp;
        currentWP = 0;

        if (tracker != null)
        {
            tracker.transform.position = transform.position;
            tracker.transform.rotation = transform.rotation;
        }
    }

    public void SetDriveOnRight(bool driveOnRight)
    {
        laneSideSign = driveOnRight ? +1f : -1f;
    }

    public void SetLane(float width, float jitter)
    {
        laneWidth = Mathf.Max(0f, width);
        laneJitter = Mathf.Max(0f, jitter);
        laneJitterValue = UnityEngine.Random.Range(-laneJitter, laneJitter);
    }

    public void SetSpeedMultiplier(float mult)
    {
        speedMult = Mathf.Max(0.1f, mult);
    }

    /// <summary>
    /// If set, car despawns when farther than radius from player.
    /// Call from CarSpawner after spawning.
    /// </summary>
    public void SetDespawnDistance(Transform player, float radius)
    {
        despawnPlayer = player;
        despawnRadiusSqr = radius > 0f ? radius * radius : -1f;
    }

    // --- Unity ---
    void Start()
    {
        tracker = new GameObject($"Tracker_{name}");
        tracker.transform.position = transform.position;
        tracker.transform.rotation = transform.rotation;

        arriveDistance *= UnityEngine.Random.Range(0.85f, 1.15f);
        laneJitterValue = UnityEngine.Random.Range(-laneJitter, laneJitter);
    }

    void OnDestroy()
    {
        if (tracker != null) Destroy(tracker);
        onDespawned?.Invoke();
    }

    void Despawn()
    {
        Destroy(gameObject);
    }

    static float DistanceXZ(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    static Vector3 NormalizeXZ(Vector3 v, Vector3 fallback)
    {
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f)
        {
            fallback.y = 0f;
            if (fallback.sqrMagnitude < 0.0001f) fallback = Vector3.forward;
            return fallback.normalized;
        }
        return v.normalized;
    }

    /// <summary>
    /// Returns a lane normal suited for axis-aligned (grid) road segments.
    /// For a segment mainly along X => lanes offset along Z.
    /// For a segment mainly along Z => lanes offset along X.
    /// </summary>
    static Vector3 ComputeGridLaneNormal(Vector3 forward)
    {
        forward = NormalizeXZ(forward, Vector3.forward);

        // Decide whether this segment is "mostly X" or "mostly Z"
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            // moving along X => right side is +/-Z depending on direction
            return Vector3.forward * Mathf.Sign(forward.x);
        }
        else
        {
            // moving along Z => right side is -/+X depending on direction
            return Vector3.right * -Mathf.Sign(forward.z);
        }
    }

    void ProgressTracker()
    {
        if (waypoints == null || waypoints.Length == 0) { Despawn(); return; }
        if (currentWP >= waypoints.Length) { Despawn(); return; }
        if (waypoints[currentWP] == null) { Despawn(); return; }

        Vector3 baseTarget = waypoints[currentWP].transform.position;

        // Segment forward (XZ only) from current position to target
        Vector3 toTarget = baseTarget - transform.position;
        Vector3 forward = NormalizeXZ(toTarget, transform.forward);

        // Lane offset perpendicular to segment direction (grid-friendly)
        Vector3 laneNormal = ComputeGridLaneNormal(forward);
        float laneOffset = laneSideSign * laneWidth + laneJitterValue;

        Vector3 targetPos = baseTarget + laneNormal * laneOffset;

        // Keep our current Y (avoid bobbing if nodes differ in height)
        targetPos.y = transform.position.y;

        // Arrive check (XZ only)
        float distToWP = DistanceXZ(transform.position, targetPos);

        // Move tracker ahead toward this target
        tracker.transform.rotation = Quaternion.LookRotation(NormalizeXZ(targetPos - tracker.transform.position, forward));
        tracker.transform.position += tracker.transform.forward * lookAhead * Time.deltaTime;

        // Advance waypoint when close enough
        if (distToWP < arriveDistance && distToWP > minArriveDistance)
        {
            currentWP++;
            if (currentWP >= waypoints.Length)
                Despawn();
        }
    }

    void Update()
    {
        // Despawn if too far from player (perf)
        if (despawnRadiusSqr > 0f && despawnPlayer != null)
        {
            Vector3 d = transform.position - despawnPlayer.position;
            d.y = 0f;
            if (d.sqrMagnitude > despawnRadiusSqr)
            {
                Despawn();
                return;
            }
        }

        if (waypoints == null || waypoints.Length == 0) return;
        if (currentWP >= waypoints.Length) return;

        ProgressTracker();
        if (currentWP >= waypoints.Length) return;

        Vector3 dir = tracker.transform.position - transform.position;
        dir.y = 0f; // yaw only
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir);

        // Rotate faster when turning more sharply
        float angle = Quaternion.Angle(transform.rotation, look);
        float dynamicRotSpeed = Mathf.Lerp(rotSpeed * 0.5f, rotSpeed, angle / 45f);

        transform.rotation = Quaternion.Slerp(transform.rotation, look, dynamicRotSpeed * Time.deltaTime);

        // Slow down on sharp turns
        float turnSharpness = Mathf.InverseLerp(0f, 90f, angle);
        float currentSpeed = Mathf.Lerp(speed, speed * 0.4f, turnSharpness) * speedMult;

        transform.Translate(0f, 0f, currentSpeed * Time.deltaTime, Space.Self);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            Vector3 a = waypoints[i].transform.position;
            Vector3 b = waypoints[i + 1].transform.position;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}