using UnityEngine;

public class WayPointFollow : MonoBehaviour
{
    public GameObject[] waypoints;
    public float speed = 10.0f;
    public float rotSpeed = 10.0f;
    public float lookAhead = 10.0f;

    private int currentWP = 0;
    private GameObject tracker;

    void Start()
    {
        // Create lightweight tracker object
        tracker = new GameObject("Tracker");
        tracker.transform.position = transform.position;
        tracker.transform.rotation = transform.rotation;
    }

    void ProgressTracker()
    {
        if (currentWP >= waypoints.Length)
        {
            Destroy(gameObject); // despawn car
            return;
        }

        Vector3 targetPos = waypoints[currentWP].transform.position;
        float distToWP = Vector3.Distance(transform.position, targetPos);

        // Move tracker toward next waypoint
        tracker.transform.LookAt(targetPos);
        tracker.transform.position += tracker.transform.forward * lookAhead * Time.deltaTime;

        // Advance to next waypoint if close enough
        if (distToWP < 3f && distToWP > 0.2f)
        {
            currentWP++;
            if (currentWP >= waypoints.Length)
            {
                Destroy(gameObject); // despawn car
            }
        }
    }

    void Update()
    {
        if (currentWP >= waypoints.Length)
            return;

        ProgressTracker();

        if (currentWP >= waypoints.Length)
            return;

        // Smooth rotation toward tracker
        Quaternion look = Quaternion.LookRotation(tracker.transform.position - transform.position);
        float angle = Quaternion.Angle(transform.rotation, look);
        float dynamicRotSpeed = Mathf.Lerp(rotSpeed * 0.5f, rotSpeed, angle / 45f);

        transform.rotation = Quaternion.Slerp(transform.rotation, look, dynamicRotSpeed * Time.deltaTime);

        // Optional: slow down on sharp turns
        float turnSharpness = Mathf.InverseLerp(0, 90f, angle);
        float currentSpeed = Mathf.Lerp(speed, speed * 0.4f, turnSharpness);

        transform.Translate(0, 0, currentSpeed * Time.deltaTime);
    }
}
