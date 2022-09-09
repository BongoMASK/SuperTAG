using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class ServerClientManager : MonoBehaviourPunCallbacks
{

    [Header("Assignables")]
    [SerializeField] PhotonView PV;
    [SerializeField] TMP_Text teamText;
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text winText;
    [SerializeField] GameObject scoreAdder;

    [Header("Score Points")]
    [SerializeField] int roundWin = 4;
    [SerializeField] int roundLose = 1;
    [SerializeField] int coolDownWin = 2;
    [SerializeField] int coolDownLose = 1;
    [SerializeField] int fallDown = -2;

    private float time;
    bool hasGivenScore;
    int checkForDennerCountdown = 10;
    int scoreCountDown = 20;
    float timer = 0;

    public bool isPaused { get; private set; }

    public bool isWaiting {
        get => GameManager.instance.playerObjectList.Count <= 1;
    }

    private void Start() 
    {
        if(!PV.IsMine)
            return;

        if (PhotonNetwork.IsMasterClient) {
            ServerGameLogicLoop();
            Invoke("CheckForDenners", checkForDennerCountdown);

            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["maxTime"];
        }

        winText.gameObject.SetActive(false);
        ChangeDennerText();
        ClientGameLogicLoop();
        
    }

    private void Update()
    {
        if (!PV.IsMine)
            return;

        if (PhotonNetwork.IsMasterClient)
            ServerGameLogicLoop();

        ClientGameLogicLoop();
    }

    #region Server Functions

    #region Time / Round Functions

    /// <summary>
    /// Syncs time to server
    /// </summary>
    void SetTime() {
        // Dont update time, if paused or waiting
        time -= Time.deltaTime;

        if (isWaiting || isPaused)
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["maxTime"];

        //send time to room
        Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;
        ht.Remove("Time");
        ht.Add("Time", time);
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }

    /// <summary>
    /// Starts a new round for all clients
    /// </summary>
    void StartNewRound() {  //resets player positions and sets time back to start
        time = (int)PhotonNetwork.CurrentRoom.CustomProperties["maxTime"];
        hasGivenScore = false;
        PV.RPC("RPC_StartNewRound", RpcTarget.All);
    }

    /// <summary>
    /// Increments round number by 1
    /// </summary>
    void IncrementRoundCount() {
        Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
        hash2.Remove("roundNumber");
        hash2.Add("roundNumber", (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"]);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);
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

    #region Score Functions

    /// <summary>
    /// Sets score to all players
    /// </summary>
    /// <param name="runnerPoints"></param>
    /// <param name="dennerPoints"></param>
    void SetScore(int runnerPoints, int dennerPoints) {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            // give points to runner
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0) {
                AddScore(PhotonNetwork.PlayerList[i], runnerPoints);
            }
            // give points to denner
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                AddScore(PhotonNetwork.PlayerList[i], dennerPoints);
            }
        }
    }

    void AddScore(Player player, int score) {
        Hashtable hash = new Hashtable();
        int currentScore = (int)player.CustomProperties["score"];
        hash.Add("score", currentScore + score);
        player.SetCustomProperties(hash);

        PV.RPC("RPC_ScoreAdder", player, score);
    }

    void GiveScoreOnCountdown() {
        if (isPaused || time < -1)
            return;

        if (time % scoreCountDown == 0)
            SetScore(coolDownWin, coolDownLose);
    }

    public void PlayerFallDownScore(Player player) {
        AddScore(player, fallDown);
    }

    #endregion

    #region Game Logic Functions

    /// <summary>
    /// Contains server logic for time, round count, pausing, etc.
    /// </summary>
    void ServerGameLogicLoop() {
        SetTime();

        if (time < 0)
            OnRoundEnd();

        if (time < -10)
            StartNewRound();

        //GiveScoreOnCountdown();
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
            if ((int)player.CustomProperties["team"] == 0) 
                runner++;
            if ((int)player.CustomProperties["team"] == 1) 
                denner++;
        }

        //if not enough denner make random person denner
        if (denner < (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 1 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if no runner, make random person runner
        else if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 0 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if dennerCount > required denners, make random person denner
        else if (denner > (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= denner - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { "team", 0 }
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
                if (surplus >= PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { "team", 1 }
                    };
                    PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
                    surplus++;
                }
            }
        }
    }

    #endregion

    #region RPCs

    [PunRPC]
    void RPC_StartNewRound() {
        //playerManager.Respawn();
    }

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
        hash.Remove("rounds");
        hash.Add("rounds", newMaxRounds);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void ChangeRoomSettings(string name, int value) {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(name)) {
            Debug.Log("no such Room setting exists");
            return;
        }

        //removing this because it causes some data inconsistency. Values dont change when they should.
        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        PhotonNetwork.CurrentRoom.CustomProperties.Remove(name);
        hash.Remove(name);
        hash.Add(name, value);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        Message.message("Changed " + name + " to: " + (int)PhotonNetwork.CurrentRoom.CustomProperties[name] + ". (enter command again if it doesnt work)");
        Invoke("CheckForDenners", 1);
    }

    public void ChangeGameTime(float newTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        PhotonNetwork.CurrentRoom.CustomProperties.Remove("maxTime");
        hash.Remove("maxTime");
        hash.Add("maxTime", newTime);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void PauseMatch() {
        if (!PhotonNetwork.IsMasterClient)
            return;
        isPaused = !isPaused;
        //Message.messageToAll("match paused", PV, RpcTarget.All);
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
        time = 1f;
        StartNewRound();

        Hashtable hash = new Hashtable {
            { "score", 0 }
        };
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);

        Hashtable hash1 = PhotonNetwork.CurrentRoom.CustomProperties;
        PhotonNetwork.CurrentRoom.CustomProperties.Remove("roundNumber");
        hash.Remove("roundNumber");
        hash.Add("roundNumber", 1);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash1);

        // PV.RPC("ResetCountdown", RpcTarget.All, (int)time - scoreCountdown);
        // TODO: call function in client info manager to reset Countdown time
    }

    #endregion


    #endregion

    #region Client Functions

    #region Time Functions

    void ClientGameLogicLoop() {
        GetTime();
        SetText();
        PlayEndRoundSound();
        //SetScoreCountdownSlider();
        if (time < 0)
            ClientRoundEnd();
    }

    void SetText() {
        // Setting Time text
        if (time > 0)
            timeText.text = ((int)time).ToString();
        else
            timeText.text = "\nRound " + (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] + " starts in " + (10 + (int)time);

        // Waiting for Players
        if (isWaiting) {
            timeText.text = "\nWaiting for Players";
        }

        // Match paused
        if (isPaused) {
            timeText.text = "\nMatch paused";
        }

        if (time < 0)
            winText.gameObject.SetActive(true);
    }

    void GetTime() {
        if (!PhotonNetwork.IsMasterClient)
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
    }

    void ClientRoundEnd() {
        AudioManager.instance.Play("Round Timer End");

        // Set WinText
        if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 0) {
            winText.text = "You WIN!";
        }
        else if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 1) {
            winText.text = "You LOSE!";
        }
    }

    /// <summary>
    /// Plays audio at the ending 10 seconds
    /// </summary>
    void PlayEndRoundSound() {
        if (time > 10 || time < 0)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f) {
            timer = 1f;
            AudioManager.instance.Play("Round Timer");
        }
    }

    //void SetScoreCountdownSlider() {
    //    if (countDownSlider.value < countDownSlider.minValue || countDownSlider.value > countDownSlider.maxValue) {
    //        countDownSlider.maxValue = time;
    //        countDownSlider.minValue = time - serverInfoManager.scoreCountDown;
    //    }
    //    countDownSlider.value = time;
    //}

    #endregion

    #region Other Functions

    /// <summary>
    /// Changes UI colour as per team
    /// </summary>
    void ChangeDennerText() {
        teamText.text = PV.Owner.CustomProperties["TeamName"].ToString();

        teamText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties["team"]];
        timeText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties["team"]];
        winText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties["team"]];
    }

    void SetPlayerProperties() {
        Hashtable hash = new Hashtable {
            { "score", 1 },
            { "team", 0 },
            { "TeamName", PlayerInfo.Instance.allTeams[0] }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public void AddScore(int adder) {
        string add = "+";
        if (adder < 0)
            add = "-";

        GameObject s = Instantiate(scoreAdder);
        if (adder > roundLose)
            s.GetComponentInChildren<TMP_Text>().color = new Color32(58, 117, 225, 255);

        s.GetComponentInChildren<TMP_Text>().text = add + Mathf.Abs(adder);
        Destroy(s, 1f);
    }

    #endregion

    #region Photon Overrides

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        if (PV.IsMine && targetPlayer == PhotonNetwork.LocalPlayer) {
            if (changedProps.ContainsKey("team")) {
                ChangeDennerText();
            }
        }
    }

    #endregion

    #endregion
}
