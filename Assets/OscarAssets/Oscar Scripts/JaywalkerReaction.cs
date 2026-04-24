using System.Collections;
using UnityEngine;
using System;

public class JaywalkerReaction : MonoBehaviour
{
    [Header("Impact Setting")]
    private float funMultiplier = 0.10f;
    private float upwardMotion = 0.8f;
    [SerializeField] private float minVehicleSpeed = 4f;

    [Header("Fine Settings")]
    [SerializeField] private float fine = 450f;
    [SerializeField] private float fineCooldownSeconds = 2f;

    private float lastFineTime = -999f;

    private Rigidbody playerRigidbody;
    private CharacterController charContrllr;
    private PlayerMovementOutside playerMovement;


    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        charContrllr = GetComponent<CharacterController>();
         playerMovement = GetComponent<PlayerMovementOutside>();

    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Vehicle"))
            return;

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

        if (vehicleSpeed < minVehicleSpeed)
            return;

        Vector3 impactDirection = (transform.position - collision.transform.position).normalized;
        Vector3 impactUpward = (impactDirection + Vector3.up * upwardMotion).normalized;

        float impactForce = vehicleMass * vehicleSpeed * funMultiplier;

        charContrllr.enabled = false;
         if (playerMovement != null) playerMovement.canMove = false; 
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.AddForce(impactUpward * impactForce, ForceMode.Impulse);

        ApplyFine();

        StartCoroutine(ResetCharContrllr());
    }

    private void ApplyFine()
    {
        if (Time.time - lastFineTime < fineCooldownSeconds)
            return;

        lastFineTime = Time.time;

        var player = PlayerService.Get();
        if (player == null)
            return;

        double actualFine = fine;
        if (actualFine <= 0.0)
            return;

        PlayerService.ApplyFine(actualFine, false);

        if (DayManager.Instance != null)
            DayManager.Instance.RegisterFine(actualFine);

        var db = DbBoot.Instance != null ? DbBoot.Instance.Db : null;
        if (db == null)
            return;

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
        if (playerMovement != null) playerMovement.canMove = true;

    }
}