using System.Collections;
using UnityEngine;

public class JaywalkerReaction : MonoBehaviour
{
    // jaywalker reaction calculates the how far a player gets thrown back when hit by a car.

    [Header("Impact Setting")]
    private float funMultiplier = 0.10f;
    private float upwardMotion = 0.8f;
    [SerializeField] private float minVehicleSpeed = 4f;

    private Rigidbody playerRigidbody;
    private CharacterController charContrllr;
    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        charContrllr = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Hit by: {collision.gameObject.name}");
        if (!collision.gameObject.CompareTag("Vehicle")) return;

        Vector3 vehicleVelocity;
        float vehicleMass;

        
        VehicleVel vehicleVel = collision.gameObject.GetComponent<VehicleVel>();
        Rigidbody vehicleRigidbody = collision.rigidbody;

        if (vehicleVel != null)
        {
            vehicleVelocity = vehicleVel.Velocity;
            vehicleMass = vehicleRigidbody != null ? vehicleRigidbody.mass : 1000f;
        }
        else if (vehicleRigidbody != null)
        {
            vehicleVelocity = vehicleRigidbody.linearVelocity;
            vehicleMass = vehicleRigidbody.mass;
        }
        else
        {
            return;
        }

        float vehicleSpeed = vehicleVelocity.magnitude;
        Debug.Log($"Speed: {vehicleSpeed}, Min: {minVehicleSpeed}, Force: {vehicleMass * vehicleSpeed * funMultiplier}");

        if (vehicleSpeed < minVehicleSpeed) return;
        Debug.Log("Launching player!");

        Vector3 impactDirection = (transform.position - collision.transform.position).normalized;
        Vector3 impactUpward = (impactDirection + Vector3.up * upwardMotion).normalized;

        float impactForce = vehicleMass * vehicleSpeed * funMultiplier;

        charContrllr.enabled = false; 
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.AddForce(impactUpward * impactForce, ForceMode.Impulse);

        StartCoroutine(ResetCharContrllr());
    }

    private IEnumerator ResetCharContrllr()
    {
        yield return new WaitForSeconds(1f);
        playerRigidbody.linearVelocity = Vector3.zero;
        charContrllr.enabled = true;
    }
}
