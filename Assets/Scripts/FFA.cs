using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Properties;

public class FFA : GameMode {

    #region Variables

    [Header("Score Points")]
    [SerializeField] int roundWin = 4;
    [SerializeField] int roundLose = 1;
    [SerializeField] int coolDownWin = 2;
    [SerializeField] int coolDownLose = 1;

    public int scoreCountDown { get => (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] / 5; }
    private int countDownMin = 9999;

    float checkForDennerCountdown = 10f;

    bool hasGivenScore = false;

    #endregion

    private void Start() {
        if (!PV.IsMine)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        time = (float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime];

        Invoke("CheckForDenners", checkForDennerCountdown);
    }

    private void Update() {
        if (!PV.IsMine || !PhotonNetwork.IsMasterClient)
            return;

        GameLogicLoop();
    }

    #region Score Functions

    /// <summary>
    /// Gives score to players every scoreCountdown seconds
    /// </summary>
    void GiveScoreOnCountdown() {
        if (isPaused || isWaiting || time < -1) {
            countDownMin = (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] - scoreCountDown;
            return;
        }

        if (time < countDownMin) {
            if (time < 1)
                return;
            SetScore(coolDownWin, coolDownLose);
            countDownMin -= scoreCountDown;
        }
    }

    #endregion

    #region Time / Round Functions 

    /// <summary>
    /// Syncs time to server
    /// </summary>
    void SetTime() {
        time -= Time.deltaTime;

        // Dont update time, if paused or waiting
        if (isWaiting || isPaused)
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime];

        //send time to room
        Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;
        ht.Remove(RoomProps.Time);
        ht.Add(RoomProps.Time, time);
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }

    /// <summary>
    /// Starts a new round for all clients
    /// </summary>
    void StartNewRound() {
        time = (float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime];
        hasGivenScore = false;
        PV.RPC("RPC_StartNewRound", RpcTarget.All);
    }

    /// <summary>
    /// Increments round number by 1
    /// </summary>
    void IncrementRoundCount() {
        int round = (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber] + 1;
        StartCoroutine(RoomUpdateSettings(RoomProps.roundNumber, round));

        if (round > (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.rounds])
            PhotonNetwork.LoadLevel("WinScreen");
    }

    /// <summary>
    /// Things to do when round ends
    /// </summary>
    void OnRoundEnd() {
        // increment rounds, set score
        if (hasGivenScore)
            return;

        IncrementRoundCount();
        SetScore(roundWin, roundLose);

        // So that it does not continue giving score
        hasGivenScore = true;
    }

    #endregion

    #region Photon Overrides

    public override void OnMasterClientSwitched(Player newMasterClient) {
        if (PhotonNetwork.IsMasterClient)
            Invoke("CheckForDenners", 2);
    }

    #endregion

    #region RPCs

    [PunRPC]
    void ChangeScoreValuesOnAll(string name, int newScore) {
        if (name == nameof(roundLose)) roundLose = newScore;
        else if (name == nameof(roundWin)) roundWin = newScore;
        else if (name == nameof(coolDownLose)) coolDownLose = newScore;
        else if (name == nameof(coolDownWin)) coolDownWin = newScore;

        else
            return;

        Message.message("Changed " + name + " to: " + newScore);
    }

    #endregion

    /// <summary>
    /// Contains server logic for time, round count, pausing, etc.
    /// </summary>
    public override void GameLogicLoop() {
        SetTime();

        if (time < 0)
            OnRoundEnd();

        if (time < -10)
            StartNewRound();

        GiveScoreOnCountdown();
    }

    /// <summary>
    /// Changes players teams on tagging
    /// </summary>
    /// <param name="player"></param>
    /// <param name="otherPlayer"></param>
    public override void HandleTag(Player player, Player otherPlayer) {
        Tag(player, otherPlayer, 1, 0);
    }

    /// <summary>
    /// Ammo handling and other networked objects
    /// </summary>
    /// <param name="p"></param>
    /// <param name="other"></param>
    public override void HandleCollision(Player p, GameObject other) {
        // Ammo Pickup
        if (other.gameObject.CompareTag("Ammo")) {
            GivePlayerAmmo(p ,other.GetComponent<AmmoPickUp>());
        }
    }
}