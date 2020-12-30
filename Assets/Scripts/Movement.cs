using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Movement from scratch by Bongo
public class Movement : MonoBehaviour
{

    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;

    //Input values
    float x, z;
    bool jumping, crouching;

    //multipliers
    float crouchMultiplier = 1f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    //Scales
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;

    private float desiredX;
    private Vector3 m_Velocity = Vector3.zero;

    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// How much to smooth out the movement

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        GetInput();
        Look();
    }

    private void FixedUpdate() {
        Move();
    }

    private void GetInput() {
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        jumping = Input.GetKey(KeyCode.Space);
        crouching = Input.GetKey(KeyCode.LeftShift);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftShift))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftShift))
            StopCrouch();
    }

    private void StartCrouch() {
        transform.localScale = crouchScale;
        crouchMultiplier = 0.5f;
    }

    private void StopCrouch() {
        transform.localScale = playerScale;
        crouchMultiplier = 1f;
    }

    private void Look() {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void Move() {
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();      //mag = magnitude
        float xMag = mag.x, yMag = mag.y;

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (z > 0 && yMag > maxSpeed) z = 0;
        if (z < 0 && yMag < -maxSpeed) z = 0;

        /// MOVEMENT OF PLAYER ----

        //target floats
        float targetSpeedx = moveSpeed * crouchMultiplier * x;
        float targetSpeedz = moveSpeed * crouchMultiplier * z;

        //target velocity
        Vector3 targetVelocityx = new Vector3(targetSpeedx, rb.velocity.y, 0f);
        Vector3 targetVelocityz = new Vector3(0f, rb.velocity.y, targetSpeedz);

        //smoothing out the movement
        rb.velocity = Vector3.SmoothDamp(rb.velocity, orientation.transform.right * targetSpeedx, ref m_Velocity, m_MovementSmoothing);
        rb.velocity = Vector3.SmoothDamp(rb.velocity, orientation.transform.forward * targetSpeedz, ref m_Velocity, m_MovementSmoothing);

        //rb.AddForce(orientation.transform.right * targetSpeedx);
        //rb.AddForce(orientation.transform.forward * targetSpeedz);

    }

    private void Jump() {
        if (grounded && readyToJump) {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            //rb.AddForce(normalVector * jumpForce * 0.5f);

           /* //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f) {
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            }
            else if (rb.velocity.y > 0) {
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
                //Debug.Log("lol bitch im overwriting your values. Z:" + rb.velocity.z);
            }*/

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump() {
        readyToJump = true;
    }

    public Vector2 FindVelRelativeToLook() {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = rb.velocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

}
