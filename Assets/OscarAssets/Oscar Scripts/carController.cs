using UnityEngine;

public class carController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;


    [Header("Wheel Meshes")]
    public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;

    [Header("Car Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeTorque = 3000f;
    public float maxSpeed = 20f;
    public float CENTER_OF_MASS_OFFSET = -0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float engineBrakeTorque = 500f;
    public float stopBrakeTorque = 1500f;
    public float creepThreshold = 1f;
    private Rigidbody rb;

    public Transform driverSeat;
    [Header("UI")]
    public GameObject useVehiclePrompt;
    public GameObject drivingInstructions;
    [HideInInspector] public bool isBeingDriven = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Vector3 centreOfMass = rb.centerOfMass;
        centreOfMass.y += CENTER_OF_MASS_OFFSET;
        rb.centerOfMass = centreOfMass;

    }


    void FixedUpdate()
    {
        if (!isBeingDriven)
        {  // if not driven: brake
            float passiveBrakes = rb.linearVelocity.magnitude < creepThreshold ? stopBrakeTorque : engineBrakeTorque;
            frontLeftWheel.brakeTorque = passiveBrakes;
            frontRightWheel.brakeTorque = passiveBrakes;
            rearLeftWheel.brakeTorque = passiveBrakes;
            rearRightWheel.brakeTorque = passiveBrakes;
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;

            UpdateWheelMeshes(frontLeftWheel, frontLeftMesh);
            UpdateWheelMeshes(frontRightWheel, frontRightMesh);
            UpdateWheelMeshes(rearLeftWheel, rearLeftMesh);
            UpdateWheelMeshes(rearRightWheel, rearRightMesh);
            return;
        }

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

        if (!isBraking && Mathf.Approximately(throttleInput, 0f))
        {
            float passiveBrake = speed < creepThreshold ? stopBrakeTorque : engineBrakeTorque;
            rearLeftWheel.brakeTorque = passiveBrake;
            rearRightWheel.brakeTorque = passiveBrake;
            frontLeftWheel.brakeTorque = 0f;
            frontRightWheel.brakeTorque = 0f;
        }
        else
        {
            float brake = isBraking ? brakeTorque : 0f;
            rearLeftWheel.brakeTorque = brake;
            rearRightWheel.brakeTorque = brake;
            frontLeftWheel.brakeTorque = brake;
            frontRightWheel.brakeTorque = brake;
        }


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
