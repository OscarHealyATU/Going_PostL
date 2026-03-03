using System;
using UnityEngine;

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
    [SerializeField] private float laneWidth = 0.5f;
    [SerializeField] private float laneJitter = 0.25f;

    private float laneSideSign = +1f; // +1 = right, -1 = left (relative to travel direction)
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
        tracker = new GameObject("Tracker");
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

    void ProgressTracker()
    {
        if (waypoints == null || waypoints.Length == 0) { Despawn(); return; }
        if (currentWP >= waypoints.Length) { Despawn(); return; }
        if (waypoints[currentWP] == null) { Despawn(); return; }

        Vector3 baseTarget = waypoints[currentWP].transform.position;

        // Travel direction for this segment (ignore Y)
        Vector3 toTarget = baseTarget - transform.position;
        toTarget.y = 0f;

        Vector3 forward = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        // GRID lane normal:
        Vector3 laneNormal;
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            // moving along X => lanes are +/-Z
            laneNormal = Vector3.forward * Mathf.Sign(forward.x);
        }
        else
        {
            // moving along Z => lanes are +/-X
            laneNormal = Vector3.right * -Mathf.Sign(forward.z);
        }

        Vector3 targetPos = baseTarget + laneNormal * (laneSideSign * laneWidth + laneJitterValue);
        targetPos.y = transform.position.y;

        float distToWP = Vector3.Distance(transform.position, targetPos);

        tracker.transform.LookAt(targetPos);
        tracker.transform.position += tracker.transform.forward * lookAhead * Time.deltaTime;

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
            if ((transform.position - despawnPlayer.position).sqrMagnitude > despawnRadiusSqr)
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
        dir.y = 0f; // YAW ONLY
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(transform.rotation, look);
        float dynamicRotSpeed = Mathf.Lerp(rotSpeed * 0.5f, rotSpeed, angle / 45f);

        transform.rotation = Quaternion.Slerp(transform.rotation, look, dynamicRotSpeed * Time.deltaTime);

        float turnSharpness = Mathf.InverseLerp(0f, 90f, angle);
        float currentSpeed = Mathf.Lerp(speed, speed * 0.4f, turnSharpness) * speedMult;

        transform.Translate(0f, 0f, currentSpeed * Time.deltaTime);
    }
}