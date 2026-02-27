using UnityEngine;

public class VehicleVel : MonoBehaviour
{
    public UnityEngine.Vector3 Velocity { get; private set; }
    private UnityEngine.Vector3 lastPos;
    void Start()
    {
        lastPos = transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Velocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;

        // Debug.Log($"Rail car velocity: {velocity.magnitude}");
    }
}
