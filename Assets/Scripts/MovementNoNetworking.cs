﻿using UnityEngine;
    
public class MovementNoNetworking : MonoBehaviour {

    //Assingables
    [Header("Assignables")]
    [SerializeField] Transform playerCam;
    [SerializeField] Transform orientation;

    //Multiplier
    float goopMultiplier = 2.5f;
    bool gooped = false;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    public static float sensitivity = 50;
    private readonly float sensMultiplier = 1f;

    //Movement
    [Header("Move Speeds")]
    [SerializeField] float moveSpeed = 4500;
    [SerializeField] float maxSpeed = 20;
    [SerializeField] bool grounded;
    [SerializeField] LayerMask whatIsGround;

    [SerializeField] float counterMovement = 0.175f;
    [SerializeField] float stopMovement = 0.3f;
    private float threshold = 0.01f;
    [SerializeField] float maxSlopeAngle = 35f;

    //Crouch & slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    [SerializeField] float slideForce = 400;
    [SerializeField] float slideCounterMovement = 0.2f;
    [SerializeField] MoveCamera moveCamera;

    //jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    [SerializeField] float jumpForce = 550f;
    [SerializeField] float jumpGraceTime;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;

    [SerializeField] float downForce = 20;

    Vector3 startPos;

    //Input
    protected struct UserInput {
        public int x, y;
        public bool jumping, sprinting, slide;
        public bool pressed;

        public bool IsPressed() {
            return x == 0 && y == 0;
        }

        public bool isShiftJumping(bool grounded, Rigidbody rb, float maxSpeed) {
            if (grounded && slide && jumping)
                return true;

            if (!grounded && jumping && rb.velocity.magnitude > maxSpeed)
                return true;

            return false;
        }
    }

    protected UserInput userInput;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    float soundTimer = 0f;

    [Header("Script References")]
    public PlayerAudio playerAudio;

    Vector3 currentGravity;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        currentGravity = Physics.gravity;
    }

    void Start() {
        playerScale = transform.localScale;
        crouchScale = playerScale;
        crouchScale.y = 0.5f;

        startPos = transform.position;        
    }

    private void FixedUpdate() {
        Movement();
        Gravity();
    }

    private void Update() {
        if (!GameManager.instance.gameIsPaused) {
            MyInput();
            Look();
        }

        //Animations(); 
        Sounds();
        StopMoving();
        Respawn();
    }

    void StopMoving() {
        if (userInput.x == 0 && userInput.y == 0 && rb.velocity.magnitude < 0.3f)
            rb.velocity = Vector3.zero;
    }

    void Gravity() {
        rb.AddForce(currentGravity, ForceMode.Acceleration);   //does gravity based on slope of floor
    }

    bool crouchSound = false;
    bool hasJumped = false;
    float currentYPos;

    void Sounds() {

        //slide
        if (userInput.slide && !userInput.jumping && grounded) {
            if (Mathf.Abs(rb.velocity.x) > 12 || Mathf.Abs(rb.velocity.z) > 12) {
                crouchSound = true;
                if (!playerAudio.GetAudioSource("Slide").isPlaying) {
                    PlaySound("Slide");
                }
            }
            else if (Mathf.Abs(rb.velocity.x) < 12 || Mathf.Abs(rb.velocity.z) < 12) {
                if (crouchSound == true) {
                    PlaySound("Slide Get Up");
                    PauseSound("Slide");
                    crouchSound = false;
                }
            }
        }
        else if ((!userInput.slide || userInput.jumping) && crouchSound == true) {
            PlaySound("Slide Get Up");
            PauseSound("Slide");

            crouchSound = false;
        }

        //Footsteps
        if (grounded && !userInput.slide) {
            if (Mathf.Abs(rb.velocity.x) > 2 || Mathf.Abs(rb.velocity.z) > 2) {
                soundTimer -= Time.deltaTime;
                if (soundTimer <= 0f) {
                    soundTimer = 0.35f;
                    GetSound();
                }
            }
        }

        //Jump
        if (!grounded && rb.velocity.y > 1) {
            hasJumped = true;
            currentYPos = transform.position.y;
        }

        if (hasJumped && grounded) {
            if (transform.position.y + 3 < currentYPos)
                PlaySound("Jump");
            hasJumped = false;
        }
    }

    void Respawn() {    //when player falls off the edge of the map
        if (transform.position.y <= startPos.y - 40f)
            transform.position = startPos;
    }

    private void MyInput() {

        if (Input.GetKey(GameManager.instance.movementKeys["right"].key))
            userInput.x = 1;     //Input.GetAxisRaw("Horizontal");
        else if (Input.GetKey(GameManager.instance.movementKeys["left"].key))
            userInput.x = -1;
        else
            userInput.x = 0;

        if (Input.GetKey(GameManager.instance.movementKeys["forward"].key))
            userInput.y = 1;
        else if (Input.GetKey(GameManager.instance.movementKeys["backward"].key))
            userInput.y = -1;
        else
            userInput.y = 0;

        userInput.jumping = Input.GetKey(GameManager.instance.movementKeys["jump"].key);
        userInput.slide = Input.GetKey(GameManager.instance.movementKeys["slide"].key);

        if (Input.GetKeyDown(GameManager.instance.otherKeys["console"].key)) {
            DebugController.showConsole = !DebugController.showConsole;
            GameManager.instance.gameIsPaused = !GameManager.instance.gameIsPaused;
            GameManager.instance.pauseMenu.SetActive(GameManager.instance.gameIsPaused);
        }

        // Sliding
        if (!gooped) {
            if (Input.GetKeyDown(GameManager.instance.movementKeys["slide"].key))
                StartCrouch();
            if (Input.GetKeyUp(GameManager.instance.movementKeys["slide"].key))
                StopCrouch();
        }

        if (gooped) {
            userInput.jumping = false;
            userInput.slide = false;
        }
    }

    private void StartCrouch() {
        ChangePlayerHeight(crouchScale);
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f)
            if (grounded)
                rb.AddForce(orientation.transform.forward * slideForce);
    }

    private void StopCrouch() {
        transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        ChangePlayerHeight(playerScale);
    }

    void ChangePlayerHeight(Vector3 scale) {
        transform.localScale = scale;
    }

    void Animations() {
        if (readyToJump && userInput.jumping && grounded) {
            Debug.Log("jumped");
            //jump animation
        }

        else if (grounded) {
            //if (rb.velocity.magnitude > 1f) {
            Debug.Log("walkin");
            //do walk animations
            //}
            //else {
            Debug.Log("Idle");
            //do idle animation
            //}
        }

        else {
            //in air animation
            if (rb.velocity.y > 0) {
                Debug.Log("goin up");
                //animation of going up
            }
            else {
                Debug.Log("comin down");
                //animation of coming down
            }
        }
    }

    private void Movement() {
        // Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * downForce);

        // Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();      //mag = magnitude

        // Counteract sliding and sloppy movement
        CounterMovement(mag);

        // If holding jump && ready to jump, then jump
        Jump();

        // Some multipliers
        float multiplier = 1f;
        float multiplierV = 1f;

        // Movement while sliding
        if (grounded && userInput.slide)
            multiplierV = 0f;

        // If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (userInput.slide && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        // Movement in air
        if (!grounded) {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * userInput.y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * userInput.x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump() {
        if (grounded && readyToJump) {
            lastGroundedTime = Time.time;
        }

        if (userInput.jumping) {
            jumpButtonPressedTime = Time.time;
        }

        //if (grounded && readyToJump) {
        if (readyToJump && Time.time - lastGroundedTime <= jumpGraceTime)
            if (Time.time - jumpButtonPressedTime <= jumpGraceTime) {
                readyToJump = false;

                //Add jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);
                rb.AddForce(orientation.transform.forward * userInput.y * moveSpeed * Time.deltaTime);

                //If userInput.jumping while falling, reset userInput.y velocity.
                Vector3 vel = rb.velocity;
                if (rb.velocity.y < 0.5f) {
                    rb.velocity = new Vector3(vel.x, 0, vel.z);
                }
                else if (rb.velocity.y > 0) {
                    rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
                }

                Invoke(nameof(ResetJump), jumpCooldown);

                lastGroundedTime = null;
                jumpButtonPressedTime = null;
            }

        // This is a bad way to fix the bug, but i have no idea whats causing it
        if (userInput.IsPressed() && userInput.jumping)
            rb.velocity = new Vector3(0, rb.velocity.y, 0);

    }

    private void ResetJump() {
        readyToJump = true;
    }

    private float desiredX;
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
        orientation.transform.rotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void CounterMovement(Vector2 mag) {

        //Slow down sliding
        if (userInput.slide) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (mag.x < -threshold && userInput.x > 0.05f || mag.x > threshold && userInput.x < -0.05f) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        else if (mag.x < -threshold && userInput.x == 0 || mag.x > threshold && userInput.x == 0) {
            // let rigidbody come to rest on its own after adding a force opposite to it
            if (grounded) {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * stopMovement);
            }
            else {
                //do nothing
            }
        }

        if (mag.y < -threshold && userInput.y > 0.05f || mag.y > threshold && userInput.y < -0.05f) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
        else if (mag.y < -threshold && userInput.y == 0 || mag.y > threshold && userInput.y == 0) {
            // let rigidbody come to rest on its own after adding a force opposite to it
            if (grounded) {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * stopMovement);
            }
            else {
                //do nothing
            }
        }

        if (userInput.isShiftJumping(grounded, rb, maxSpeed))
            return;

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-userInput.crouching, so not optimal.
        if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)) > maxSpeed) {
            if (grounded || userInput.jumping) {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * maxSpeed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
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

    private bool IsFloor(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other) {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++) {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal)) {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;

                currentGravity = -normal * Physics.gravity.magnitude;

                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded) {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void OnTriggerEnter(Collider other) {
        //goop gun effects
        if (other.CompareTag("Goop")) {
            maxSpeed = goopMultiplier;
            gooped = true;
        }
    }

    private void OnTriggerExit(Collider collision) {
        if (collision.CompareTag("Goop")) {
            maxSpeed = 20;
            gooped = false;
            if (!userInput.slide) ChangePlayerHeight(playerScale);
        }
    }

    void PlaySound(string name) {
        playerAudio.Play(name);
    }

    void PauseSound(string name) {
        playerAudio.Pause(name);
    }

    void GetSound() {
        playerAudio.PlayRandomFootstep();
    }

    private void StopGrounded() {
        grounded = false;
        currentGravity = Physics.gravity;
    }
}
