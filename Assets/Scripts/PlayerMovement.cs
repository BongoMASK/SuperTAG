using System;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using TMPro;

public class PlayerMovement : MonoBehaviourPunCallbacks, IDamageable {

    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Multiplier
    float goopMultiplier = 2.5f;
    public bool gooped = false;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    public static float sensitivity = 50;
    private float sensMultiplier = 1f;

    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.175f;
    public float stopMovement = 0.3f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    //Input
    float x, y;
    bool jumping, sprinting, crouching, mouseDown;
    KeyCode switchPrev;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    //Items
    [SerializeField] Item[] items;

    int itemIndex;
    int previousItemIndex = -1;
    int prevWeapon = -1;

    //Photon Networking Variables
    PhotonView PV;

    public TMP_Text InfoText;
    private Renderer renderer;
    AudioManager audioManager;

    float soundTimer = 0f;

    [SerializeField] Material[] material;
    [SerializeField] GameObject glasses;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject tagFeed;
    [SerializeField] Transform tagFeedList;

    //step climb objects
    [SerializeField] GameObject stepUpper;
    [SerializeField] GameObject stepLower;
    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] float stepsmooth = 0.1f;

    public PlayerAudio playerAudio;

    [SerializeField] const float maxHealth = 100f;
    float currentHealth = maxHealth;

    Vector3 currentGravity;

    PlayerManager playerManager;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        PV = GetComponent<PhotonView>();
        renderer = GetComponent<Renderer>();
        currentGravity = Physics.gravity;

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

        stepUpper.transform.position = new Vector3(stepUpper.transform.position.x, stepHeight, stepUpper.transform.position.z);
    }

    void Start() {
        if (PV.IsMine) {
            EquipItem(1);
            GetComponent<MeshRenderer>().enabled = false;
            Destroy(glasses);
        }
        else {
            Destroy(playerCam.gameObject);
            Destroy(rb);
            renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            return;
        }

        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString();

        countdownStart = (int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"];

        FindObjectOfType<AudioManager>().Play("Breeze");
    }

    private void FixedUpdate() {
        if (!PV.IsMine) {
            return;
        }
        Movement();
        //StepClimb(0.1f, 0.15f);
    }

    private void Update() {
        if (!PV.IsMine) {
            return;
        }

        if (countdown > -0.5f) { //So that it doesnt keep doing the countdown to infinity
            countdown -= Time.deltaTime;
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\nTag Cooldown: " + (int)countdown;
        }

        if (countdown <= 0)
            InfoText.gameObject.SetActive(false);

        if (!GameManager.gameIsPaused) {
            MyInput();
            Look();
            ChangeItem();
        }

        if (canvas != null) {
            canvas.SetActive(!GameManager.gameIsPaused);
        }

        Gravity();
        //Animations(); 
        Sounds();
    }

    void Gravity() {
        rb.AddForce(currentGravity, ForceMode.Force);
    }

    bool crouchSound = false;

    void Sounds() {
        //Slide
        if (crouching && !jumping && grounded) {
            if (Mathf.Abs(rb.velocity.x) > 3 || Mathf.Abs(rb.velocity.z) > 3) {
                crouchSound = true;
                if (!playerAudio.GetAudioSource("Slide").isPlaying) {
                    PV.RPC("RPC_PlaySound", RpcTarget.AllBuffered, GetComponent<PhotonView>().ViewID, "Slide");
                }
            }
            else if (Mathf.Abs(rb.velocity.x) < 3 || Mathf.Abs(rb.velocity.z) < 3) {
                //FindObjectOfType<AudioManager>().Play("Slide Get Up");
                //FindObjectOfType<AudioManager>().Pause("Slide");

                PV.RPC("RPC_PlaySound", RpcTarget.AllBuffered, GetComponent<PhotonView>().ViewID, "Slide Get Up");
                PV.RPC("RPC_PauseSound", RpcTarget.AllBuffered, GetComponent<PhotonView>().ViewID, "Slide");
                crouchSound = false;
            }
        }
        else if ((!crouching || jumping) && crouchSound == true) {
            //FindObjectOfType<AudioManager>().Play("Slide Get Up");
            //FindObjectOfType<AudioManager>().Pause("Slide");

            PV.RPC("RPC_PlaySound", RpcTarget.AllBuffered, GetComponent<PhotonView>().ViewID, "Slide Get Up");
            PV.RPC("RPC_PauseSound", RpcTarget.AllBuffered, GetComponent<PhotonView>().ViewID, "Slide");

            crouchSound = false;
        }

        //Footsteps
        if (grounded && !crouching) {
            if (Mathf.Abs(rb.velocity.x) > 2 || Mathf.Abs(rb.velocity.z) > 2) {
                soundTimer -= Time.deltaTime;
                if (soundTimer <= 0f) {
                    //FindObjectOfType<AudioManager>().PlayRandomFootstep();
                    soundTimer = 0.35f;
                    PV.RPC("RPC_GetSound", RpcTarget.All, GetComponentInChildren<PhotonView>().ViewID);
                    //playerAudio.PlayRandomFootstep();
                }
            }
        }

        //Jump
        if (grounded && readyToJump && jumping) {
            PV.RPC("RPC_PlaySound", RpcTarget.AllBuffered, GetComponentInChildren<PhotonView>().ViewID, "Jump");
            //FindObjectOfType<AudioManager>().Play("Jump");
        }

        //Breeze
        if (!FindObjectOfType<AudioManager>().GetAudioSource("Breeze").isPlaying) {
            FindObjectOfType<AudioManager>().Play("Breeze");
        }
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput() {

        if (Input.GetKey(GameManager.GM.movementKeys["right"].key))
            x = 1;     //Input.GetAxisRaw("Horizontal");
        else if (Input.GetKey(GameManager.GM.movementKeys["left"].key))
            x = -1;
        else {
            x = 0;
        }

        if (Input.GetKey(GameManager.GM.movementKeys["forward"].key))
            y = 1;
        else if (Input.GetKey(GameManager.GM.movementKeys["backward"].key))
            y = -1;
        else {
            y = 0;
        }


        switchPrev = GameManager.GM.movementKeys["prevWeapon"].key;
        jumping = Input.GetKey(GameManager.GM.movementKeys["jump"].key);
        crouching = Input.GetKey(GameManager.GM.movementKeys["crouch"].key);

        if (Input.GetKeyDown(GameManager.GM.movementKeys["console"].key)) {
            DebugController.showConsole = !DebugController.showConsole;
            GameManager.gameIsPaused = !GameManager.gameIsPaused;
        }

        //Crouching
        if (!gooped) {
            if (Input.GetKeyDown(GameManager.GM.movementKeys["crouch"].key))
                StartCrouch();
            if (Input.GetKeyUp(GameManager.GM.movementKeys["crouch"].key))
                StopCrouch();
        }

        if (gooped) {
            jumping = false;
            crouching = false;
        }
    }

    private void StartCrouch() {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f) {
            if (grounded) {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch() {
        transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        transform.localScale = playerScale;
    }

    void Animations() {
        if (readyToJump && jumping && grounded) {
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

    void ChangeItem() {
        for (int i = 0; i < items.Length; i++) {
            if (Input.GetKeyDown(GameManager.GM.itemKeys[i].key)) {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) {
            if (itemIndex >= items.Length - 1) {
                EquipItem(0);
            }
            else {
                EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) {
            if (itemIndex <= 0) {
                EquipItem(items.Length - 1);
            }
            else {
                EquipItem(itemIndex - 1);
            }
        }

        //switch to prev weapon
        if (Input.GetKeyDown(GameManager.GM.movementKeys["prevWeapon"].key)) {
            EquipItem(prevWeapon);
        }

        if (Input.GetMouseButton(0)) {
            items[itemIndex].Use();
        }

    }

    void EquipItem(int _index) {
        if (_index == previousItemIndex) {
            return;
        }
        prevWeapon = itemIndex;
        itemIndex = _index;

        if (items.Length > 0) {
            items[itemIndex].itemGameObject.SetActive(true);
        }

        if (previousItemIndex != -1 && items.Length > 0) {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (PV.IsMine) {
            Hashtable hash = new Hashtable {
                { "itemIndex", itemIndex }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        if (!PV.IsMine && targetPlayer == PV.Owner) {
            if (changedProps.ContainsKey("itemIndex")) {
                EquipItem((int)changedProps["itemIndex"]);
            }
            if (changedProps.ContainsKey("team")) {
                renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            }
        }

        if (PV.IsMine && targetPlayer == PhotonNetwork.LocalPlayer) {
            if (changedProps.ContainsKey("team")) {
                ChangeOnTeamsChange();
            }
        }
    }

    void ChangeOnTeamsChange() {
        countdown = countdownStart;
        renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
        FindObjectOfType<AudioManager>().Play("TagSound");

        if (InfoText != null) {
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\nTag Cooldown: " + (int)countdown;
            InfoText.gameObject.SetActive(true);

            if (PV.Owner.CustomProperties["team"] != null) {
                GetComponent<TeamSetup>().isDennerText.text = PV.Owner.CustomProperties["TeamName"].ToString();
            }
        }
    }

    [SerializeField] float downForce = 20;

    private void Movement() {
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * downForce);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();      //mag = magnitude
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        /*if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;*/

        //Some multipliers
        float multiplier = 1f;
        float multiplierV = 1f;

        // Movement in air
        if (!grounded) {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    [SerializeField] float jumpGraceTime;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;

    private void Jump() {
        if (grounded && readyToJump) {
            lastGroundedTime = Time.time;
        }

        if (jumping) {
            jumpButtonPressedTime = Time.time;
        }

        //if (grounded && readyToJump) {
        if (readyToJump && Time.time - lastGroundedTime <= jumpGraceTime)
            if (Time.time - jumpButtonPressedTime <= jumpGraceTime) {
                readyToJump = false;

                //Add jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);
                rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime);

                //If jumping while falling, reset y velocity.
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

    private void CounterMovement(float x, float y, Vector2 mag) {
        //if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (mag.x < -threshold && x > 0.05f || mag.x > threshold && x < -0.05f) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        else if (mag.x < -threshold && x == 0 || mag.x > threshold && x == 0) {
            // let rigidbody come to rest on its own after adding a force opposite to it
            if (grounded) {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * stopMovement);
            }
            else {
                //do nothing
            }
        }

        if (mag.y < -threshold && y > 0.05f || mag.y > threshold && y < -0.05f) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
        else if (mag.y < -threshold && y == 0 || mag.y > threshold && y == 0) {
            // let rigidbody come to rest on its own after adding a force opposite to it
            if (grounded) {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * stopMovement);
            }
            else {
                //do nothing
            }
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)) > maxSpeed) {
            if (grounded || jumping) {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * maxSpeed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
        }
    }

    void StepClimb(float raycastLength, float raycastLength2) {
        CreateStepClimbDirections(orientation.transform.forward, raycastLength, raycastLength2);
        CreateStepClimbDirections(new Vector3(1.5f, 0, 1), raycastLength, raycastLength2);
        CreateStepClimbDirections(new Vector3(-1.5f, 0, 1), raycastLength, raycastLength2);
    }

    void CreateStepClimbDirections(Vector3 direction, float raycastLength, float raycastLength2) {
        RaycastHit lower;
        if (Physics.Raycast(stepLower.transform.position, transform.TransformDirection(direction), out lower, raycastLength)) {
            RaycastHit upper;
            if (!Physics.Raycast(stepUpper.transform.position, transform.TransformDirection(direction), out upper, raycastLength2)) {
                rb.position -= new Vector3(0f, -stepsmooth, 0f);
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

    float countdown = 5f;
    public float countdownStart = 5f;

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player") && countdown <= 0f) {
            if ((int)PV.Owner.CustomProperties["team"] == 1 &&
            (int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties["team"] == 0) {

                // calling function to master client because only the master client can change the custom properties of other players
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, 1);

                ChangeMyTeam(0);
                countdown = countdownStart;

                //SendFeedToAll(PhotonNetwork.LocalPlayer, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner);
                PV.RPC("SpawnTagFeed", RpcTarget.All, PhotonNetwork.LocalPlayer, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner);
            }
            else if ((int)PV.Owner.CustomProperties["team"] == 0 &&
                (int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties["team"] == 1) {

                // calling function to master client because only the master client can change the custom properties of other players
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, 0);

                ChangeMyTeam(1);
                countdown = countdownStart;

                //SendFeedToAll(PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner, PhotonNetwork.LocalPlayer);
                PV.RPC("SpawnTagFeed", RpcTarget.All, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner, PhotonNetwork.LocalPlayer);
            }
        }

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
        }
    }

    [PunRPC]
    void RPC_SwitchPlayerTeam(int viewID, int team) {
        Hashtable hash = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };

        PhotonView.Find(viewID).Owner.SetCustomProperties(hash);
    }

    void SendFeedToAll(Player player1, Player player2) {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            PV.RPC("SpawnTagFeed", PhotonNetwork.PlayerList[i], player1, player2);
        }
    }

    void ChangeMyTeam(int team) {
        Hashtable hash2 = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash2);
    }

    [PunRPC]
    void RPC_PlaySound(int viewID, string name) {
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.Play(name);
    }

    [PunRPC]
    void RPC_PauseSound(int viewID, string name) {
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.Pause(name);
    }

    [PunRPC]
    void SpawnTagFeed(Player player1, Player player2) {
        DisplayTagFeed(player1, player2);
    }

    [PunRPC]
    void RPC_GetSound(int viewID) {
        /*AudioSource source = testAudioSource;//PhotonView.Find(viewID).gameObject.GetComponent<AudioSource>();
        FindObjectOfType<AudioManager>().PlayOthersFootsteps(source);*/
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.PlayRandomFootstep();
    }

    void DisplayTagFeed(Player player1, Player player2) {
        //if (!PV.IsMine) return;
        Debug.Log(player1.NickName + " tagged " + player2.NickName);

        GameObject t = Instantiate(tagFeed);

        if (tagFeedList != null) {
            t.transform.SetParent(tagFeedList);
            if (tagFeedList.childCount > 4) {
                Destroy(tagFeedList.GetChild(0));
            }
        }
        else {
            Debug.Log("no tagfeed lol. view ID" + PV.ViewID);
        }

        t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " tagged " + player2.NickName;
        Destroy(t, 7f);
    }

    private void StopGrounded() {
        grounded = false;
        currentGravity = Physics.gravity;
    }

    public void TakeDamage(float damage) {
        PV.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage) {
        if (!PV.IsMine) return;

        Debug.Log("took damage " + damage);
        currentHealth -= damage;

        if (currentHealth <= 0f) {
            Die();
        }
    }

    void Die() {
        playerManager.Die();
    }

    public void ChangeValues(string name, int newValue) {
        if (!PhotonNetwork.IsMasterClient) return;

        PV.RPC("RPC_ChangeValuesOnAll", RpcTarget.AllBuffered, name, newValue);
    }

    [PunRPC]
    void RPC_ChangeValuesOnAll(string name, int newValue) {
        if (name == nameof(maxSpeed)) maxSpeed = newValue;
        if (name == nameof(jumpForce)) jumpForce = newValue;
        if (name == nameof(slideForce)) slideForce = newValue;
        if (name == nameof(downForce)) downForce = newValue;
    }
}
