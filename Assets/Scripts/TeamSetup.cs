using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
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
    public TMP_Text scoreText; 

    public float time;
    float timer;
    float timeUntilRestart = 10f;

    float checkForDennerCountdown = 10f;
    float scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6;

    int roundNumber = 1;

    bool hasWon = false;

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
        scoreText.gameObject.SetActive(false);

        SetDenners();

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
    }

    void Update() {

        if(!PV.IsMine) {
            return;
        }

        GameOver();

        if (PhotonNetwork.IsMasterClient && time >= 0f) {
            scoreCountdown -= Time.deltaTime;
            if (scoreCountdown <= 0f) {
                SetScore();
                scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6; 
            }

            checkForDennerCountdown -= Time.deltaTime;
            if (checkForDennerCountdown <= 0f && PhotonNetwork.IsMasterClient) {
                CheckForDenners();
                checkForDennerCountdown = 10f;
            }
        }
    }

    void SetScore() {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            Hashtable hash = new Hashtable();
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0) {   
                hash.Add("score", (int)PhotonNetwork.PlayerList[i].CustomProperties["score"] + 2);
            }
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                hash.Add("score", (int)PhotonNetwork.PlayerList[i].CustomProperties["score"] + 1);
            }
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);
        }
    }

    void GameOver() {
        if (PhotonNetwork.IsMasterClient) {
            time -= Time.deltaTime;

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;  //send time to room
            ht.Remove("Time");
            ht.Add("Time", time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else {
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1) {
            TimeText.text = "Waiting For Players";
            time = (int)PV.Owner.CustomProperties["time"];
            scoreCountdown = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6;
        }
        else {
            if ((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] < 0f && WinText != null) {        //after round finishes 
                timeUntilRestart -= Time.deltaTime;

                if((float)PhotonNetwork.CurrentRoom.CustomProperties["Time"] > -0.1f) {
                    FindObjectOfType<AudioManager>().Play("Round Timer End");
                }

                if(PhotonNetwork.IsMasterClient) {
                    SetCurrentRound();
                }

                if (hasWon == true) {
                    if (PhotonNetwork.IsMasterClient) {
                        //sends round number over network
                        roundNumber++;

                        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                            Hashtable hash = new Hashtable();
                            if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 0) {
                                hash.Add("score", (int)PhotonNetwork.PlayerList[i].CustomProperties["score"] + 4);
                            }
                            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties["team"] == 1) {
                                hash.Add("score", (int)PhotonNetwork.PlayerList[i].CustomProperties["score"] + 1);
                            }
                            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);
                        }
                    }
                    else {
                        roundNumber = (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"];
                    }

                    if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 0) {
                        WinText.text = "You WIN!";
                    }
                    else if ((int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 1) {
                        WinText.text = "You LOSE!";
                    }
                    hasWon = false;     //so that it doesnt keep adding the score
                }

                scoreText.text = "Score:  " + (int)PV.Owner.CustomProperties["score"];

                if ((int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] <= 4) {
                    TimeText.text = "Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] + " in " + (int)(10 + time);
                }
                else {
                    TimeText.text = "Final Round in " + (int)timeUntilRestart;
                }

                if ((int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] >= 6 && PhotonNetwork.IsMasterClient) {
                    SceneManager.LoadScene("WinScreen");
                }

                if (time <= -10) {
                    StartNewRound();
                }
                else if (time <= -9.9 && !PhotonNetwork.IsMasterClient) {
                    StartNewRound();
                }
                //TODO: check whether it works without this.

                WinText.gameObject.SetActive(true);
                scoreText.gameObject.SetActive(true);
            }
                
            if (time >= 0f) {
                MatchTimerStart();
            }
        }
    }

    void SetCurrentRound() {
        Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
        hash2.Remove("roundNumber");
        hash2.Add("roundNumber", roundNumber);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash2); 
    }

    public void StartNewRound() {  //resets player positions and stuff
        if (PhotonNetwork.IsMasterClient) {
            time = (int)PV.Owner.CustomProperties["time"];
        }
        else {
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }
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
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1 || !PhotonNetwork.IsMasterClient) {
            return;
        }

        int runner = 0, denner = 0;

        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties["team"] == 0) {
                runner++;
            }
            else {
                denner++;
            }
        }

        if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 0 },
                { "TeamName", PlayerInfo.Instance.allTeams[0] }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }
        else if (denner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { "team", 1 },
                { "TeamName", PlayerInfo.Instance.allTeams[1] }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }
        else if (denner > (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= denner - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
                    return;
                }
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
        else if (runner >= PhotonNetwork.CurrentRoom.PlayerCount) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]) {
                    return;
                }
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

    public void LeaveRoom() {
        PhotonNetwork.Disconnect();       //need to disconnect the player before we change scenes
        SceneManager.LoadScene(0);
    }

    void MatchTimerStart() {    //sends timer over the network
        timeUntilRestart = 10f;
        hasWon = true;
        timer -= Time.deltaTime;

        if (time <= 10 && time >= 0 && timer <= 0f) {
            timer = 1f;
            FindObjectOfType<AudioManager>().Play("Round Timer");
        }

        if (WinText != null) {
            TimeText.text = ((int)(float)PhotonNetwork.CurrentRoom.CustomProperties["Time"]).ToString();
            WinText.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(false);
        }
    }
}
