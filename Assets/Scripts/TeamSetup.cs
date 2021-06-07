using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class TeamSetup : MonoBehaviourPunCallbacks {
    private PhotonView PV;

    public TMP_Text isDennerText;
    public TMP_Text TimeText;
    public TMP_Text WinText;
    public TMP_Text PlayerNameText;

    [SerializeField] PlayerNetworking playerNetworking;

    [SerializeField] GameObject canvas;

    [SerializeField] GameObject scoreAdder;

    [SerializeField] int roundWin, roundLose, coolDownWin, coolDownLose, fallDown;

    public float time;
    float timer;

    float checkForDennerCountdown = 10f;
    float scoreCountdown;

    int roundNumber = 1;
    int maxRounds = 5;

    bool hasWon = false, isPaused = false;
    public static bool disableHUD = true;

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    void Start() {
        if (!PV.IsMine) {
            PlayerNameText.text = PV.Owner.NickName;
            return;
        }
        else {
            Destroy(PlayerNameText.gameObject);
        }

        isDennerText.text = PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString();

        WinText.gameObject.SetActive(false);

        SetDenners();

        maxRounds = (int)PhotonNetwork.CurrentRoom.CustomProperties["rounds"];
 
        if (PhotonNetwork.IsMasterClient) {
            time = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"];
            Hashtable ht = new Hashtable {
                { "Time", time }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else {
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }

        scoreCountdown = time / 6;
        GameManager.GM.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
    }

    void Update() {

        if(!PV.IsMine) {
            return;
        }

        GameOver();
        Respawn();

        if (PhotonNetwork.IsMasterClient && time >= 0f) {
            scoreCountdown -= Time.deltaTime;
            if (scoreCountdown <= 0f) {
                SetScore();
                scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6; 
            }

            if (Time.time - checkForDennerCountdown > 10) {     //checks for denners every 10 seconds
                CheckForDenners();
                checkForDennerCountdown = Time.time;
            }
        }

        if(Input.GetKeyDown(GameManager.GM.otherKeys["hideUI"].key)) {
            DisableHUD();
        }
    }

    void DisableHUD() {
        disableHUD = !disableHUD;
        canvas.GetComponent<Canvas>().enabled = disableHUD;
        GameManager.GM.GetComponentInChildren<Canvas>().enabled = disableHUD;
    }

    [PunRPC]
    void ScoreAdder(int adder) {
        string add = "+";
        if(adder < 0)
            add = "-";

        GameObject s = Instantiate(scoreAdder);
        if(adder > coolDownLose && adder > roundLose)
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

    void SetScore() {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0) {
                AddScore(PhotonNetwork.PlayerList[i], coolDownWin);
            }
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                AddScore(PhotonNetwork.PlayerList[i], coolDownLose);
            }
        }
    }

    void Respawn() {    //when player falls off the edge of the map
        if (transform.position.y <= -40f) {
            transform.position = new Vector3(0f, 0f, 0f);
            PV.RPC("AddScore", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, fallDown);
        }
    }

    void GameOver() {
        if (PhotonNetwork.IsMasterClient && !isPaused) {     //time is set here
            time -= Time.deltaTime;

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;  //send time to room
            ht.Remove("Time");
            ht.Add("Time", time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1) {
            TimeText.text = "Waiting For Players";
            time = (int)PV.Owner.CustomProperties["time"];
            scoreCountdown = time / 6;
        }
        else {
            if ((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] < 0f && WinText != null) {        //after round finishes 

                if ((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] > -0.1f) {
                    playerNetworking.audioManager.Play("Round Timer End");
                }

                if (hasWon == true) {
                    if (PhotonNetwork.IsMasterClient) {
                        //sends round number over network
                        if (PhotonNetwork.IsMasterClient) SetCurrentRound(roundNumber);
                        roundNumber++;

                        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                            Hashtable hash = new Hashtable();
                            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0)
                                AddScore(PhotonNetwork.PlayerList[i], roundWin);

                            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) 
                                AddScore(PhotonNetwork.PlayerList[i], roundLose);

                            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);
                        }
                    }
                    else {
                        //this statement is required, when master client leaves server.
                        roundNumber = (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"];
                    }
                    GameManager.GM.roundText.text = "Round " + roundNumber;

                    if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 0) {
                        WinText.text = "You WIN!";
                    }
                    else if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 1) {
                        WinText.text = "You LOSE!";
                    }
                    hasWon = false;     //so that it doesnt keep adding the score
                }

                //Rounds checking
                if (roundNumber <= maxRounds - 1)
                    TimeText.text = "Round " + roundNumber + " in " + (int)(10 + time);

                else
                    TimeText.text = "Final Round in " + (int)(10 + time);

                if (roundNumber >= maxRounds + 1 && PhotonNetwork.IsMasterClient) SceneManager.LoadScene("WinScreen");

                if (time <= -10) StartNewRound();

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
        if (PhotonNetwork.IsMasterClient) time = (int)PV.Owner.CustomProperties["time"];
        else time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        
        scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6;

        Vector3 spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));
        transform.position = spawnPosition;
    }

    void SetDenners() {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }

        int value = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]);

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++) {
            Hashtable hash2 = new Hashtable {
                { "team", 0 },
                { "TeamName", PlayerInfo.Instance.allTeams[0] }
            };
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
        }

        for (int i = 0; i < (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]; i++) {
            Hashtable hash2 = new Hashtable {
                { "team", 1 },
                { "TeamName", PlayerInfo.Instance.allTeams[1] }
            };
            PhotonNetwork.PlayerList[value + i].SetCustomProperties(hash2);
        }
    }

    void CheckForDenners() {
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1 || !PhotonNetwork.IsMasterClient) return;

        int runner = 0, denner = 0;

        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties["team"] == 0) runner++;
            else denner++;
        }

        //if no runner, make random person runner
        if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 0 },
                { "TeamName", PlayerInfo.Instance.allTeams[0] }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if no denner make random person denner
        else if (denner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 1 },
                { "TeamName", PlayerInfo.Instance.allTeams[1] }
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
            playerNetworking.audioManager.Play("Round Timer");
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

    [PunRPC]
    void ChangeScoreValuesOnAll(string name, int newScore) {
        if (name == nameof(roundLose)) roundLose = newScore;
        if (name == nameof(roundWin)) roundWin = newScore;
        if (name == nameof(coolDownLose)) coolDownLose = newScore;
        if (name == nameof(coolDownWin)) coolDownWin = newScore;
        if (name == nameof(fallDown)) fallDown = newScore;
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

        scoreCountdown = time / 6;
        GameManager.GM.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
        GameManager.GM.roundText.text = "Round " + roundNumber;
    }

    public void ChangeGameTime(float newTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log((int)PV.Owner.CustomProperties["time"]);
        Hashtable hash = PV.Owner.CustomProperties;
        hash.Remove("time");
        hash.Add("time", newTime);
        PV.Owner.SetCustomProperties(hash);

        scoreCountdown = time / 6;
        GameManager.GM.coolDownText.text = "Score Cooldown: " + (int)scoreCountdown;
    }

    public void PauseMatch() {
        if (!PhotonNetwork.IsMasterClient) return;
        isPaused = !isPaused;
    }

    public void RestartRound() {
        if (!PhotonNetwork.IsMasterClient) return;
        StartNewRound();
    }

    public void RestartGame() {
        if (!PhotonNetwork.IsMasterClient) return;
        time = 1f;
        StartNewRound();
        roundNumber = 0;
        GameManager.GM.roundText.text = "Round " + roundNumber;

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            Hashtable hash = new Hashtable {
                { "score", 0 }
            };
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);
        }
    }
}
