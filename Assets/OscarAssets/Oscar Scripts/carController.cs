using UnityEngine;

public class carController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeTorque = 3000f;
    public float maxSpeed = 200f;
    public float CENTER_OF_MASS_OFFSET = -0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Rigidbody rb;
    [HideInInspector] public bool isBeingDriven = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
       Vector3 com =  rb.centerOfMass; 
       com.y += CENTER_OF_MASS_OFFSET;
         rb.centerOfMass = com;

    }

    
    void FixedUpdate()
    {
        if (!isBeingDriven) return;
        float speed = rb.linearVelocity.magnitude; 
        float throttleInput = Input.GetAxis("Vertical");
        float steeringInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        float steeringAngle = steeringInput * maxSteeringAngle;
        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;

        if (!isBraking && speed < maxSpeed)
        {
            rearLeftWheel.motorTorque = throttleInput * maxMotorTorque;
            rearRightWheel.motorTorque = throttleInput * maxMotorTorque;
        } 
        else
        {
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
        } 

        float brake = isBraking ? brakeTorque : 0f;
        rearLeftWheel.brakeTorque = brake;
        rearRightWheel.brakeTorque = brake;
        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;

        UpdateWheelMeshes(frontLeftWheel, frontLeftMesh);
        UpdateWheelMeshes(frontRightWheel, frontRightMesh);
        UpdateWheelMeshes(rearLeftWheel, rearLeftMesh);
        UpdateWheelMeshes(rearRightWheel, rearRightMesh);

    }

    void UpdateWheelMeshes(WheelCollider wheelCollider, Transform wheelMesh)
    {
        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelMesh.position = position;
        wheelMesh.rotation = rotation;
    }
}
