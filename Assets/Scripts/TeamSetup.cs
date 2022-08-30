using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class TeamSetup : MonoBehaviourPunCallbacks {

    #region Variables

    private PhotonView PV;

    public TMP_Text isDennerText;
    public TMP_Text TimeText;
    public TMP_Text WinText;
    public TMP_Text PlayerNameText;

    [SerializeField] Slider countDownSlider;

    [SerializeField] GameObject canvas;

    [SerializeField] GameObject scoreAdder;

    [SerializeField] int roundWin, roundLose, coolDownWin, coolDownLose;

    public float time;
    float timer;

    float checkForDennerCountdown = 10f;
    float scoreCountdown;

    int roundNumber = 1;
    int maxRounds = 5;
    int multiplier = 5;

    bool hasWon = false, isPaused = false;
    public static bool disableHUD = true;

    bool isOnline = true;

    #endregion

    private void Awake() {
        if (SceneManager.GetActiveScene().name == "Tutorial") {
            PhotonNetwork.OfflineMode = true;
            isOnline = false;
        }
        else {
            PhotonNetwork.OfflineMode = false;
            isOnline = true;
        }

        PV = GetComponent<PhotonView>();

        EnableText();
    }

    void Start() {
        if (!PV.IsMine && isOnline) {
            PlayerNameText.text = PV.Owner.NickName;
            return;
        }
        else {
            Destroy(PlayerNameText.gameObject);
        }

        isDennerText.text = PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString();
        WinText.gameObject.SetActive(false);

        maxRounds = (int)PhotonNetwork.CurrentRoom.CustomProperties["rounds"];

        if (PhotonNetwork.IsMasterClient) {
            time = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"];
            Hashtable ht = new Hashtable {
                { "Time", time }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else {
            PV.RPC("RPC_GetMaxTime", RpcTarget.MasterClient, PV.ViewID);
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }

        //scorecountdown setting options
        scoreCountdown = 1 / multiplier;
        scoreCountdownDivider = (int)(time / scoreCountdown);
        GameManager.instance.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
        countDownSlider.maxValue = 1001;
        countDownSlider.minValue = 1000;
    }

    void Update() {
        if (!PV.IsMine && isOnline)
            return;

        if (isOnline)
            GameOver();
 
        if (Input.GetKeyDown(GameManager.instance.otherKeys["hideUI"].key)) 
            DisableHUD();

        if (Time.time - checkForDennerCountdown > 10) {     //checks for denners every 10 seconds
            CheckForDenners();
            checkForDennerCountdown = Time.time;
        }

        if (time > 0)
            ScoreCountdown();
    }

    int scoreCountdownDivider;

    void EnableText() {
        isDennerText.enabled = !PhotonNetwork.OfflineMode;
        WinText.enabled = !PhotonNetwork.OfflineMode;
        TimeText.enabled = !PhotonNetwork.OfflineMode;
        countDownSlider.gameObject.SetActive(!PhotonNetwork.OfflineMode);
    }

    void DisableHUD() {
        disableHUD = !disableHUD;
        canvas.GetComponent<Canvas>().enabled = disableHUD;
        GameManager.instance.GetComponentInChildren<Canvas>().enabled = disableHUD;
    }

    #region Network Functions

    void SetScoreOnCountdown() {
        if (isPaused) return;
        if (GameManager.instance.playerObjectList.Count <= 1) return;
        if (!PhotonNetwork.IsMasterClient) return;

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            // give *coolDownWin* points to runner
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0) {
                AddScore(PhotonNetwork.PlayerList[i], coolDownWin);
            }
            // give *coolDownLose* points to denner
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                AddScore(PhotonNetwork.PlayerList[i], coolDownLose);
            }
        }
    }

    void SetScoreOnRoundOver() {

    }

    void UpdateRoundTime() {

    }

    

    // TODO: Clean up this function
    void GameOver() {
        if (PhotonNetwork.IsMasterClient && !isPaused) {     //time is set here
            time -= Time.deltaTime;

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;  //send time to room
            ht.Remove("Time");
            ht.Add("Time", time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else 
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];

        //when there arent enough players in the game
        if (GameManager.instance.playerObjectList.Count <= 1) {
            TimeText.text = "\nWaiting For Players";
            time = (int)PV.Owner.CustomProperties["time"];
            scoreCountdown = time / multiplier;
            countDownSlider.value = 0;
            ResetCountdown((int)time - (int)scoreCountdown);
        }
        else {
            countDownSlider.value = time;
            if ((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] < 0f && WinText != null) {        //after round finishes 

                if ((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] > -0.1f) {
                    AudioManager.instance.Play("Round Timer End");
                }

                if (hasWon == true) {
                    if (PhotonNetwork.IsMasterClient) {
                        //sends round number over network
                        if (PhotonNetwork.IsMasterClient) 
                            SetCurrentRound(roundNumber);

                        roundNumber++;

                        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0)
                                AddScore(PhotonNetwork.PlayerList[i], roundWin);

                            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1)
                                AddScore(PhotonNetwork.PlayerList[i], roundLose);
                        }
                    }
                    else {
                        //this statement is required, when master client leaves server.
                        roundNumber = (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"];
                    }
                    GameManager.instance.roundText.text = "Round " + roundNumber;

                    if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 0) {
                        WinText.text = "You WIN!";
                    }
                    else if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 1) {
                        WinText.text = "You LOSE!";
                    }

                    //resetting the scoreCountdown Slider values.
                    ResetCountdown((int)PhotonNetwork.LocalPlayer.CustomProperties["time"] - (int)scoreCountdown);

                    hasWon = false;     //so that it doesnt keep adding the score
                }

                //Rounds checking
                if (roundNumber <= maxRounds - 1)
                    TimeText.text = "Round " + roundNumber + " in " + (int)(10 + time);

                else
                    TimeText.text = "Final Round in " + (int)(10 + time);

                if (roundNumber >= maxRounds + 1 && PhotonNetwork.IsMasterClient) SceneManager.LoadScene("WinScreen");

                // Start new round when time <= -10
                if (time <= -10 && PhotonNetwork.IsMasterClient) 
                    StartNewRound();

                WinText.gameObject.SetActive(true);
            }

            if (time >= 0f) MatchTimerStart();
        }
    }

    void SetCurrentRound(int roundNumber) {
        Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
        hash2.Remove("roundNumber");
        hash2.Add("roundNumber", roundNumber);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);
    }
    
    public void StartNewRound() {  //resets player positions and sets time back to start
        PV.RPC("RPC_StartNewRound", RpcTarget.All);
    }

    /// <summary>
    /// Checks if there are more or less denners
    /// </summary>
    void CheckForDenners() {
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1 || !PhotonNetwork.IsMasterClient) return;

        int runner = 0, denner = 0;

        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties["team"] == 0) runner++;
            else denner++;
        }

        //if not enough denner make random person denner
        if (denner < (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 1 },
                { "TeamName", PlayerInfo.Instance.allTeams[1] }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if no runner, make random person runner
        else if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 0 },
                { "TeamName", PlayerInfo.Instance.allTeams[0] }
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
                        { "team", 0 },
                        { "TeamName", PlayerInfo.Instance.allTeams[0] }
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
                        { "team", 1 },
                        { "TeamName", PlayerInfo.Instance.allTeams[1] }
                    };
                    PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
                    surplus++;
                }
            }
        }
    }

    /*public override void OnPlayerLeftRoom(Player otherPlayer) {
        if(!PhotonNetwork.IsMasterClient) {
            return;
        }
        if (otherPlayer != player) {
            CheckForDenners();
            //Invoke("CheckForDenners", 2.0f);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }
        if (newPlayer != player) {
            CheckForDenners();
            //Invoke("CheckForDenners", 2.0f);
        }
    }*/

    void MatchTimerStart() {    //does timer for audio at last 10 secs
        hasWon = true;
        timer -= Time.deltaTime;

        if (time <= 10 && timer <= 0f) {
            timer = 1f;
            AudioManager.instance.Play("Round Timer");
        }

        if (WinText != null) {
            TimeText.text = ((int)(float)PhotonNetwork.CurrentRoom.CustomProperties["Time"]).ToString();
            WinText.gameObject.SetActive(false);
        }
    }

    public void ChangeScoreValues(string name, int newScore) {
        if (!PhotonNetwork.IsMasterClient) return;

        PV.RPC("ChangeScoreValuesOnAll", RpcTarget.AllBuffered, name, newScore);
    }

    public void ChangeTime(int newTime) {
        if (!PhotonNetwork.IsMasterClient) return;
        time = newTime;
    }

    public void ChangeMaxRounds(int newMaxRounds) {
        if (!PhotonNetwork.IsMasterClient) return;
        maxRounds = newMaxRounds;
        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        hash.Remove("rounds");
        hash.Add("rounds", maxRounds);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        scoreCountdown = time / multiplier;
        GameManager.instance.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
        GameManager.instance.roundText.text = "Round " + roundNumber;
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

        Debug.Log((int)PV.Owner.CustomProperties["time"]);
        Hashtable hash = PV.Owner.CustomProperties;
        hash.Remove("time");
        hash.Add("time", newTime);
        PV.Owner.SetCustomProperties(hash);

        scoreCountdown = time / multiplier;
        GameManager.instance.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
        countDownSlider.maxValue = scoreCountdown;
    }

    public void PauseMatch() {
        if (!PhotonNetwork.IsMasterClient) return;
        isPaused = !isPaused;
        //Message.messageToAll("match paused", PV, RpcTarget.All);
    }

    public void RestartRound() {
        if (!PhotonNetwork.IsMasterClient) return;
        StartNewRound();
        PV.RPC("ResetCountdown", RpcTarget.All, (int)time - 20);
    }

    public void RestartGame() {
        if (!PhotonNetwork.IsMasterClient) return;
        time = 1f;
        StartNewRound();
        roundNumber = 1;
        GameManager.instance.roundText.text = "Round " + roundNumber;

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            Hashtable hash = new Hashtable {
                { "score", 0 }
            };
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);
        }
        PV.RPC("ResetCountdown", RpcTarget.All, (int)time - scoreCountdown);
    }

    /// <summary>
    /// Handles countdown for Assigning Scores to each player
    /// </summary>
    void ScoreCountdown() {
        if (time < countDownSlider.minValue) {
            scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / multiplier;
            GameManager.instance.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;

            scoreCountdownDivider = (int)(time / scoreCountdown);
            countDownSlider.maxValue = (scoreCountdownDivider + 1) * scoreCountdown;
            countDownSlider.minValue = scoreCountdownDivider * scoreCountdown;
            SetScoreOnCountdown();
        }
    }

    #endregion

    #region Remote Procedural Callbacks RPCs

    [PunRPC]
    void ScoreAdder(int adder) {
        string add = "+";
        if (adder < 0)
            add = "-";

        GameObject s = Instantiate(scoreAdder);
        if (adder > coolDownLose && adder > roundLose)
            s.GetComponentInChildren<TMP_Text>().color = new Color32(58, 117, 225, 255);

        s.GetComponentInChildren<TMP_Text>().text = add + Mathf.Abs(adder);
        Destroy(s, 1f);
    }

    [PunRPC]
    void AddScore(Player player, int score) {
        Hashtable hash = new Hashtable();
        int currentScore = (int)player.CustomProperties["score"];
        hash.Add("score", currentScore + score);
        player.SetCustomProperties(hash);

        PV.RPC("ScoreAdder", player, score);
    }

    [PunRPC]
    void RPC_GetMaxTime(int viewID) {
        int t = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"];
        Hashtable hash = PhotonView.Find(viewID).Owner.CustomProperties;
        hash.Remove("time");
        hash.Add("time", t);
        PhotonView.Find(viewID).Owner.SetCustomProperties(hash);
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

    /// <summary>
    /// resets the countdown slider. Useful when starting a new round.
    /// </summary>
    /// <param name="time"></param>
    [PunRPC]
    private void ResetCountdown(int time) {
        scoreCountdownDivider = (int)(time / scoreCountdown);
        countDownSlider.maxValue = time + scoreCountdown;
        countDownSlider.minValue = time;
    }

    [PunRPC]
    void RPC_StartNewRound() {
        if (PhotonNetwork.IsMasterClient) 
            time = (int)PV.Owner.CustomProperties["time"];
        else 
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];

        Vector3 spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));
        transform.position = spawnPosition;
    }

    #endregion
}
