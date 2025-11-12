using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    Vector2 movementVector;

    [SerializeField]
    private Transform orientation;

    [Header("Movement Settings")]
    public float movementSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    // bool readyToJump;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //readyToJump = true;
    }

    void Update()
    {
        // ground check (casts a ray from current pos downwards, checks if it hits something)
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        // handle drag
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }

        SpeedControl();

        //Debug.Log(rb.linearVelocity);
    }

    void FixedUpdate()
    {
        // calculate movement direction
        Vector3 movement = orientation.forward * movementVector.y +
        orientation.right * movementVector.x;

        // on ground
        if (grounded)
        {
            rb.AddForce(movement.normalized * movementSpeed * 10f, ForceMode.Force);
        }
        // on air
        else
        {
            rb.AddForce(movement.normalized * movementSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    public void OnMove(InputValue val)
    {
        movementVector = val.Get<Vector2>();
    }

    // public void OnJump()
    // {
    //     if (readyToJump && grounded)
    //     {
    //         readyToJump = false;
    //         // reset y velocity
    //         rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    //         rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

    //         // resets the readyToJump boolean after a set amount of time
    //         Invoke(nameof(ResetJump), jumpCooldown);
    //     }
    // }

    // void ResetJump()
    // {
    //     readyToJump = true;
    // }

    void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // limit velocity if needed to match correct movement speed
        if (flatVelocity.magnitude > movementSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * movementSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }
}
