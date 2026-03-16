using UnityEngine;

public class PlayerMovementOutside : MonoBehaviour
{
    [Header("Movement Settings (will be removed after testing)")]
    public float runSpeed = 30f;
    private float acceleration = 10f;
    private float jumpHeight = 4f;
    // gravity = (9.81^2)/2, exaggerated gravity to make jumps feel less floaty.    
    private float gravity = -48.11805f;
    
     [Header("Ground Check Settings")]
    public Transform groundCheck;
    private float groundCheckRadius = 0.4f;
    public LayerMask groundLayer;
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 curVelocity;
    private bool isGrounded;
    [HideInInspector]   public bool canMove = true;
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
     if(!canMove)
        {
            curVelocity = Vector3.zero;
            return;
        }
        
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // current velocity is Slerped to the target velocity with the aim of nicer movement - has a side effect of chaotic movement when switching between forward and backward movement
        // switch to SmoothDamp if it interferes with gameplay too much ~ Oscar
        Vector3 targetVelocity = (transform.right * moveX + transform.forward * moveZ).normalized * runSpeed;
        curVelocity = Vector3.Slerp(curVelocity, targetVelocity, acceleration * Time.deltaTime);

        controller.Move(curVelocity * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
