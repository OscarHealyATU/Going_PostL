using System;
using UnityEngine;

public class enterCar : MonoBehaviour
{

    private float enterDistance = 3f;
   
    [Header("Player Handling")]
    
    private carController currentCar;
    private bool isInCar = false;
    private CharacterController playerController;
    private PlayerMovementOutside playerMovement;
    private Rigidbody playerRigidBody;
    public PlayerLook playerLook;

    private Vector3 playerHeight;


    void Start()
    {
        playerController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovementOutside>();
        playerRigidBody = GetComponent<Rigidbody>();
        playerHeight = playerLook.cameraRoot.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInCar)
        {
            currentCar = FindCar();
            if (currentCar != null)
            {
                // prompts player to enter into a vehicle
                currentCar.UseVehiclePrompt.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F)) EnterCar();
            }
            else
            {
                HidePrompts();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F)) ExitCar();
        }
    }

     void HidePrompts()
    {
        carController[] cars = FindObjectsByType<carController>(FindObjectsSortMode.None);
        foreach (carController car in cars)
        {
            if(car.UseVehiclePrompt != null) car.UseVehiclePrompt.SetActive(false);
        }
    }

    carController FindCar()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, enterDistance);
        foreach (Collider hit in hits)
        {
            carController car = hit.GetComponentInParent<carController>();
            if (car != null) return car;
        }
        return null;
    }

    void EnterCar()
    {
        currentCar.UseVehiclePrompt.SetActive(false);

        isInCar = true;
        currentCar.isBeingDriven = true;
        
        playerMovement.canMove = false;
        playerLook.canLook = false;
        playerController.enabled = false;
        playerRigidBody.isKinematic = true;

        playerLook.cameraRoot.SetParent(currentCar.driverSeat);
        playerLook.cameraRoot.localPosition = Vector3.zero;
        playerLook.cameraRoot.localRotation = Quaternion.identity;
        transform.position = new Vector3(0,-500,0);
    }

    void ExitCar()
    {
        
        isInCar = false;
        currentCar.isBeingDriven = false;

        playerLook.cameraRoot.SetParent(transform);
        playerLook.cameraRoot.localPosition = playerHeight;
        playerLook.cameraRoot.localRotation = Quaternion.identity;
        transform.position = currentCar.transform.position + currentCar.transform.right * 2f;

        playerMovement.canMove = true;
        playerLook.canLook = true;
        playerController.enabled = true;
        playerRigidBody.isKinematic = false;

        currentCar.UseVehiclePrompt.SetActive(false);
        currentCar = null;
    }

    void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enterDistance);
    }
}
