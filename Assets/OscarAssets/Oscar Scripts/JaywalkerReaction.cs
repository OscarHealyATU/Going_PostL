using System.Collections;
using UnityEngine;
using System.Linq;
using System;

public class JaywalkerReaction : MonoBehaviour
{
    // jaywalker reaction calculates the how far a player gets thrown back when hit by a car.

    [Header("Impact Setting")]
    private float funMultiplier = 0.10f;
    private float upwardMotion = 0.8f;
    [SerializeField] private float minVehicleSpeed = 4f;
    [Header("Fine Settings")]
    [SerializeField] private float fine = 50f;
    [SerializeField] private float fineCooldownSeconds = 2f;

    private float lastFineTime = -999f;

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
        // //debug.Log($"Hit by: {collision.gameObject.name}");
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
        //debug.Log($"Speed: {vehicleSpeed}, Min: {minVehicleSpeed}, Force: {vehicleMass * vehicleSpeed * funMultiplier}");

        if (vehicleSpeed < minVehicleSpeed) return;
        //debug.Log("Launching player!");

        Vector3 impactDirection = (transform.position - collision.transform.position).normalized;
        Vector3 impactUpward = (impactDirection + Vector3.up * upwardMotion).normalized;

        float impactForce = vehicleMass * vehicleSpeed * funMultiplier;

        charContrllr.enabled = false; 
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.AddForce(impactUpward * impactForce, ForceMode.Impulse);
        ApplyFine();
        StartCoroutine(ResetCharContrllr());
    }

    private void ApplyFine()
    {
        if (Time.time - lastFineTime < fineCooldownSeconds) return;
        lastFineTime = Time.time;


        if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
        {
            Debug.LogWarning("[JaywalkerReaction] DB not ready, skipping fine");
            return;
        }

        var db = DbBoot.Instance.Db;
        var player = db.Table<Player>().FirstOrDefault();
        if (player == null) return;

        float actualFine = Mathf.Min(fine, (float)player.money);
        if (actualFine <= 0f)
        {
            Debug.Log("[JaywalkerReaction] Player has no money left");
            return;
        }

        player.money -= actualFine;
        db.Update(player);

        db.Insert(new TransactionLog
        {
            playerId = player.id,
            type = "fine_jaywalking",
            amount = -actualFine,
            description = "Hit by vehicle",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
    

    private IEnumerator ResetCharContrllr()
    {
        yield return new WaitForSeconds(1f);
        playerRigidbody.linearVelocity = Vector3.zero;
        charContrllr.enabled = true;
    }
}
