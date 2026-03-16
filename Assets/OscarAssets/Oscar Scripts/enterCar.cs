using UnityEngine;
using Unity.Cinemachine;

public class enterCar : MonoBehaviour
{

    private float enterDistance = 3f;
    [Header("Cinemachine Handling")]
    public Camera playerCam;

    [Header("Player Handling")]
    public Transform driverSeat;
    private carController currentCar;
    private bool isInCar = false;
    private CharacterController playerController;
    private PlayerMovementOutside playerMovement;
    private PlayerLook playerLook;


    void Start()
    {
        playerController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovementOutside>();
        playerLook = GetComponent<PlayerLook>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInCar)
        {
            currentCar = FindCar();
            if (currentCar != null && Input.GetKeyDown(KeyCode.F)) EnterCar();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F)) ExitCar();
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
        isInCar = true;
        currentCar.isBeingDriven = true;
        
        playerMovement.canMove = false;
        playerLook.canLook = false;
        playerController.enabled = false;

        transform.SetParent(driverSeat);
        transform.position = driverSeat != null ? driverSeat.position : currentCar.transform.position;
    }

    void ExitCar()
    {
        isInCar = false;
        currentCar.isBeingDriven = false;

        transform.SetParent(null);
        transform.position = currentCar.transform.position + currentCar.transform.right * 2f;

        playerMovement.canMove = true;
        playerMovement.enabled = true;
        playerLook.canLook = true;
    }

    void OGizmosSelected()
    {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enterDistance);
    }
}
