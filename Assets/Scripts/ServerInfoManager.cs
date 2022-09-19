using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using Properties;

public class ServerInfoManager : MonoBehaviourPunCallbacks
{
    #region Variables

    [Header("Assignables")]
    [SerializeField] PhotonView PV;
    [SerializeField] ClientInfoManager clientInfoManager;
    [SerializeField] PlayerManager playerManager;

    [Header("Score Points")]
    [SerializeField] int roundWin = 4;
    [SerializeField] int roundLose = 1;
    [SerializeField] int coolDownWin = 2;
    [SerializeField] int coolDownLose = 1;
    [SerializeField] int fallDown = -2;

    public int scoreCountDown { get => (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] / 5; }
    private int countDownMin = 9999;

    private float time;

    private bool isWaiting { get => GameManager.instance.playerObjectList.Count <= 1; }
    private bool isPaused = false;
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

    private void Update()
    {
        if (!PV.IsMine || !PhotonNetwork.IsMasterClient)
            return;

        GameLogicLoop();
    }

    #region Score Functions

    /// <summary>
    /// Sets score to all players
    /// </summary>
    /// <param name="runnerPoints"></param>
    /// <param name="dennerPoints"></param>
    void SetScore(int runnerPoints, int dennerPoints) {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            // give points to runner
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 0)
                AddScore(PhotonNetwork.PlayerList[i], runnerPoints);

            // give points to denner
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1)
                AddScore(PhotonNetwork.PlayerList[i], dennerPoints);
        }
    }

    void AddScore(Player player, int score) {
        Hashtable hash = new Hashtable();
        int currentScore = (int)player.CustomProperties[PlayerProps.score];
        hash.Add(PlayerProps.score, currentScore + score);
        player.SetCustomProperties(hash);

        PV.RPC("RPC_ScoreAdder", player, score);
    }

    /// <summary>
    /// Gives score to players every scoreCountdown seconds
    /// </summary>
    void GiveScoreOnCountdown() {
        if (isPaused || isWaiting || time < -1) {
            countDownMin = (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] - scoreCountDown;
            return;
        }

        if(time < countDownMin) {
            if (time < 1)
                return;
            SetScore(coolDownWin, coolDownLose);
            countDownMin -= scoreCountDown;
        }
    }

    public void PlayerFallDownScore(Player player) {
        AddScore(player, fallDown);
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
    /// Keep checking if roundCounter has increased or not to ensure round change
    /// </summary>
    /// <returns></returns>
    IEnumerator RoomUpdateSettings(string key, int value) {

        while ((int)PhotonNetwork.CurrentRoom.CustomProperties[key] != value) {
            Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
            hash2.UpdateHashtable(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);

            Debug.Log((int)PhotonNetwork.CurrentRoom.CustomProperties[key]);

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// Keep checking if roundCounter has increased or not to ensure round change
    /// </summary>
    /// <returns></returns>
    IEnumerator RoomUpdateSettings(string key, float value) {

        while ((float)PhotonNetwork.CurrentRoom.CustomProperties[key] != value) {
            Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
            hash2.UpdateHashtable(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);

            Debug.Log((float)PhotonNetwork.CurrentRoom.CustomProperties[key]);

            yield return new WaitForSeconds(1);
        }
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

    #region Game Logic Functions

    /// <summary>
    /// Contains server logic for time, round count, pausing, etc.
    /// </summary>
    void GameLogicLoop() {
        SetTime();

        if (time < 0)
            OnRoundEnd();

        if (time < -10)
            StartNewRound();

        GiveScoreOnCountdown();
    }

    /// <summary>
    /// Checks if there are more or less denners
    /// </summary>
    void CheckForDenners() {
        Invoke("CheckForDenners", checkForDennerCountdown);

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1 || !PhotonNetwork.IsMasterClient) return;

        int runner = 0, denner = 0;

        // Check runner and denner count
        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties[PlayerProps.team] == 0)
                runner++;
            else if ((int)player.CustomProperties[PlayerProps.team] == 1)
                denner++;
        }

        //if not enough denner make random person denner
        if (denner < (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { PlayerProps.team, 1 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if no runner, make random person runner
        else if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { PlayerProps.team, 0 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if dennerCount > required denners, make random person denner
        else if (denner > (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= denner - (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { PlayerProps.team, 0 }
                    };
                    PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
                    surplus++;
                }
            }
        }

        //if runnerCount is more than required, make random person denner
        else if (runner >= PhotonNetwork.CurrentRoom.PlayerCount) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { PlayerProps.team, 1 }
                    };
                    PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
                    surplus++;
                }
            }
        }
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

    #region Console Commands

    public void ChangeScoreValues(string name, int newScore) {
        if (!PhotonNetwork.IsMasterClient) return;

        PV.RPC("ChangeScoreValuesOnAll", RpcTarget.AllBuffered, name, newScore);
    }

    public void ChangeTime(int newTime) {
        if (!PhotonNetwork.IsMasterClient) return;
        time = newTime;
    }

    public void ChangeMaxRounds(int newMaxRounds) {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        hash.Remove(RoomProps.rounds);
        hash.Add(RoomProps.rounds, newMaxRounds);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void ChangeRoomSettings(string name, int value) {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(name)) {
            Debug.Log("no such Room setting exists");
            return;
        }

        //removing this because it causes some data inconsistency. Values dont change when they should.
        //Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        //PhotonNetwork.CurrentRoom.CustomProperties.Remove(name);
        //hash.Remove(name);
        //hash.Add(name, value);
        //PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        StartCoroutine(RoomUpdateSettings(name, value));

        Message.message("Changed " + name + " to: " + (int)PhotonNetwork.CurrentRoom.CustomProperties[name]);
        Invoke("CheckForDenners", 1);
    }

    public void ChangeGameTime(int newTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        //Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        //PhotonNetwork.CurrentRoom.CustomProperties.Remove(RoomProps.maxTime);
        //hash.Remove(RoomProps.maxTime);
        //hash.Add(RoomProps.maxTime, (float)newTime);
        //PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        StartCoroutine(RoomUpdateSettings(RoomProps.maxTime, (float)newTime));
    }

    public void PauseMatch() {
        if (!PhotonNetwork.IsMasterClient)
            return;
        isPaused = !isPaused;
    }

    public void RestartRound() {
        if (!PhotonNetwork.IsMasterClient)
            return;

        StartNewRound();
        // PV.RPC("ResetCountdown", RpcTarget.All, (int)time - scoreCountdown);
        // TODO: call function in client info manager to reset Countdown time
    }

    public void RestartGame() {
        if (!PhotonNetwork.IsMasterClient) return;
        StartNewRound();

        Hashtable hash = new Hashtable {
            { PlayerProps.score, 0 }
        };
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);

        StartCoroutine(RoomUpdateSettings(RoomProps.roundNumber, 1));

        // PV.RPC("ResetCountdown", RpcTarget.All, (int)time - scoreCountdown);
        // TODO: call function in client info manager to reset Countdown time
    }

    #endregion
}
