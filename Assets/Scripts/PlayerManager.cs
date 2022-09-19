using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Properties;
using TMPro;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    #region Variables

    public PhotonView PV { get; private set; }

    [SerializeField] ServerInfoManager serverInfo;
    [SerializeField] ClientInfoManager clientInfo;
    [SerializeField] GameObject tagFeed;

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
        if (!PV.IsMine) {
            Destroy(spectatorCam.gameObject);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
            CreateController();
        else
            SendFeedToAll(PhotonNetwork.LocalPlayer, null, "joined as Spectator");
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

        if (spectatorCam != null)
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
        GameModeManager.gameMode.PlayerFallDownScore(player);
    }

    /// <summary>
    /// Called on Specific player by MasterClient to show score going down
    /// </summary>
    /// <param name="score"></param>
    [PunRPC]
    void RPC_ScoreAdder(int score) {
        clientInfo.AddScore(score);
    }

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

    /// <summary>
    /// Spawns tag feed on client
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    /// <param name="text"></param>
    [PunRPC]
    void SpawnTagFeed(Player player1, Player player2 = null, string text = "") {
        DisplayTagFeed(player1, player2, text);
    }

    [PunRPC]
    void RPC_HandleTag(Player p1, Player p2) {
        GameModeManager.gameMode.HandleTag(p1, p2);
    }

    [PunRPC]
    void RPC_HandleCollision(Player p1, GameObject other) {
        GameModeManager.gameMode.HandleCollision(p1, other);
    }

    [PunRPC]
    void RPC_ForceSpectator() {

        CreateSpectator();
    }

    #endregion

    #region Photon Overrides

    // Calls "Player is connecting"
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        if (!PV.IsMine) return;

        SpawnTagFeed(newPlayer, null, "is connecting");
    }

    // Calls "Player has disconnected"
    public override void OnPlayerLeftRoom(Player player) {
        if (!PV.IsMine) return;

        SpawnTagFeed(player, null, "has disconnected");
    }

    // Calls "Player is new Server Host"
    public override void OnMasterClientSwitched(Player newMasterClient) {
        if (!PV.IsMine) return;

        SpawnTagFeed(newMasterClient, null, "is the new Server Host");
    }

    #endregion

    #region Player Functions

    /// <summary>
    /// Spawns tag feed for client
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    /// <param name="text"></param>
    void DisplayTagFeed(Player player1, Player player2 = null, string text = "") {
        GameObject t = Instantiate(tagFeed);

        t.transform.SetParent(GameManager.instance.tagFeedList);
        if (GameManager.instance.tagFeedList.childCount > 4) {
            Destroy(GameManager.instance.tagFeedList.GetChild(0).gameObject);
        }

        Destroy(t, 10f);
        if (player2 == null) {
            t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " <#FFF>" + text;
            return;
        }
        t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " <#FFF>tagged<#FF0000> " + player2.NickName;
    }

    /// <summary>
    /// Spawns tag feed across all clients
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    /// <param name="text"></param>
    void SendFeedToAll(Player player1, Player player2 = null, string text = "") {
        PV.RPC("SpawnTagFeed", RpcTarget.All, player1, player2, text);
    }

    /// <summary>
    /// Changes player team
    /// </summary>
    /// <param name="team"></param>
    private void ChangeMyTeam(int team) {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, team }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }
    
    #endregion
}

// TODO: Make Tag Feed only accessible through Player Manager
// TODO: Make isTaggable Player CustomProperty