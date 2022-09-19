using Photon.Pun;
using Photon.Realtime;
using Properties;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;

public class Elimination : GameMode {

    #region Variables

    bool hasGivenScore = false;
    float checkForDennerCountdown = 10f;

    /// <summary>
    /// Gets number of runners in game
    /// </summary>
    int runnerCount {
        get {
            int r = 0;
            foreach (Player player in PhotonNetwork.PlayerList) 
                if ((int)player.CustomProperties[PlayerProps.team] == 0)
                    r++;
            return r;
        }
    }

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
        //SetScore(0, (int)time);

        // So that it does not continue giving score
        hasGivenScore = true;
    }

    /// <summary>
    /// Called when time hits zero
    /// </summary>
    void OnTimeHitsZero() {
        if (runnerCount < 1)
            return;

        // Set time to room time
        time = (float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime];
        // isPaused = true;

        string dennerName = "%Error%"; 
        Player currentDenner = null;
        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties[PlayerProps.team] == 1) {
                dennerName = player.NickName;
                currentDenner = player;
                break;
            }
        }

        Message.message(dennerName + " has been eliminated");

        // Destroy Denner 
        if (currentDenner != null)
            PV.RPC("RPC_ForceSpectator", currentDenner);

        // Set new Denner
        Invoke("CheckForDenners", 0.5f);
    }

    #endregion

    #region Photon Overrides

    public override void OnMasterClientSwitched(Player newMasterClient) {
        if (PhotonNetwork.IsMasterClient)
            Invoke("CheckForDenners", 2);
    }

    #endregion

    public override void GameLogicLoop() {
        SetTime();

        // End game if there are no runners left
        if (runnerCount < 1)
            OnRoundEnd();

        if (time < 0)
            OnTimeHitsZero();

        if (time < -10)
            StartNewRound();
    }

    public override void HandleCollision(Player p, GameObject other) {
        
    }

    public override void HandleTag(Player p, Player other) {
        Tag(p, other, 1, 0);
    }
}
