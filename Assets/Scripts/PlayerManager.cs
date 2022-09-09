using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Properties;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    #region Variables

    public PhotonView PV { get; private set; }

    [SerializeField] ServerInfoManager serverInfo;
    [SerializeField] ClientInfoManager clientInfo;

    GameObject controller;

    [SerializeField] Camera spectatorCam;
    [SerializeField] float specCamDist = 15;
    Transform currentPlayer = null;

    int currentPlayerIndex = 0;

    public bool isSpectator { 
        get => controller == null; 
    }

    float desiredX;
    float xRotation;

    #endregion

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    private void Start() {
        if (PV.IsMine) {
            if (PhotonNetwork.IsMasterClient)
                CreateController();
            return;
        }
        Destroy(spectatorCam.gameObject);
    }

    private void LateUpdate() {
        if (!PV.IsMine)
            return;

        FollowPlayer();
    }

    #region Controller Functions

    /// <summary>
    /// Instantiates Player into the scene
    /// </summary>
    public void CreateController() {
        if (!PV.IsMine)
            return;
        
        if (controller != null)
            return;

        ChangeMyTeam(1);
        currentPlayer = null;
        spectatorCam.gameObject.SetActive(false);

        controller = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "PlayerContainer 1"), GetSpawnPosition(), Quaternion.identity, 0, new object[] { PV.ViewID });
    }

    public void Die() {
        PhotonNetwork.Destroy(controller);
        CreateSpectator();
    }

    public void Respawn() {
        if (controller == null)
            return;
        controller.transform.GetChild(0).position = GetSpawnPosition();
    }

    public void OnFall(Player player) {
        PV.RPC("RPC_Fall", RpcTarget.MasterClient, player);
    }

    private Vector3 GetSpawnPosition() {
        Vector3 spawnPosition;
        spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));

        SpawnPoints[] spawnPositions = FindObjectsOfType<SpawnPoints>();

        if (spawnPositions.Length == 0)
            return spawnPosition;
        else
            return spawnPositions[Random.Range(0, spawnPositions.Length)].transform.position;
    }

    #endregion

    #region Spectator Functions

    /// <summary>
    /// Instantiates Player as a Spectator
    /// </summary>
    public void CreateSpectator() {
        if (controller != null)
            PhotonNetwork.Destroy(controller);

        spectatorCam.gameObject.SetActive(true);
        ChangePlayerSpectating(10);

        ChangeMyTeam(2);
    }

    void FollowPlayer() {
        if (currentPlayer == null) {
            if (isSpectator)
                ChangePlayerSpectating(0);
            return;
        }

        MouseInput();

        #region Camera Clipping

        float distanceOffset;
        Vector3 relativePos = spectatorCam.transform.position - currentPlayer.position;
        RaycastHit hit;

        if (Physics.Raycast(currentPlayer.position, relativePos, out hit, specCamDist + 5f)) {
            Debug.DrawLine(currentPlayer.position, hit.point);
            distanceOffset = specCamDist - hit.distance + 0.8f;
            distanceOffset = Mathf.Clamp(distanceOffset, 0, specCamDist);
        }
        else
            distanceOffset = 0;

        #endregion

        spectatorCam.transform.position = spectatorCam.transform.rotation * new Vector3(0.0f, 0.0f, -specCamDist + distanceOffset) + currentPlayer.position;
    }

    void ChangePlayerSpectating(int i) {
        if(GameManager.instance.playerObjectList.Count == 0)
            return;
        currentPlayerIndex = i % GameManager.instance.playerObjectList.Count;
        currentPlayer = GameManager.instance.playerObjectList[currentPlayerIndex];
    }

    void MouseInput() {
        if (GameManager.instance.gameIsPaused)
            return;

        float mouseX = Input.GetAxis("Mouse X") * 50 * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * 50 * Time.fixedDeltaTime;

        //Find current look rotation
        Vector3 rot = spectatorCam.transform.rotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        spectatorCam.transform.rotation = Quaternion.Euler(xRotation, desiredX, 0);

        if (Input.GetKeyDown(GameManager.instance.otherKeys["fire"].key))
            ChangePlayerSpectating(currentPlayerIndex + 1);
    }

    #endregion

    #region RPCs

    /// <summary>
    /// Called on MasterClient when player falls down
    /// </summary>
    /// <param name="player"></param>
    [PunRPC]
    void RPC_Fall(Player player) {
        serverInfo.PlayerFallDownScore(player);
    }

    /// <summary>
    /// Called on Specific player by MasterClient to show score going down
    /// </summary>
    /// <param name="score"></param>
    [PunRPC]
    void RPC_ScoreAdder(int score) {
        clientInfo.AddScore(score);
    }
    // TODO: Do it with onPlayerPropsChange()

    [PunRPC]
    void RPC_StartNewRound() {
        Respawn();
    }

    /// <summary>
    /// Changes team of player. Only called on master client
    /// </summary>
    /// <param name="viewID"></param>
    /// <param name=PlayerProps.team></param>
    [PunRPC]
    void RPC_SwitchPlayerTeam(int viewID, int team) {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, team }
        };
        PhotonView.Find(viewID).Owner.SetCustomProperties(hash);
    }

    #endregion

    private void ChangeMyTeam(int team) {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, team }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }
}
