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

    Player player;

    float time;
    float timeUntilRestart = 10f;

    float checkForDennerCountdown = 10f;
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
        GameOver();
        checkForDennerCountdown -= Time.deltaTime;
        if (checkForDennerCountdown <= 0f) {
            CheckForDenners();
            checkForDennerCountdown = 10f;
        }
    }

    void GameOver() {
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1) {
            TimeText.text = "Waiting For Players";
            time = (int)PV.Owner.CustomProperties["time"];
        }
        else {
            if (time <= 0f && WinText != null) {        //after round finishes 
                timeUntilRestart -= Time.deltaTime;

                TimeText.text = "Round " + roundNumber + " in " + (int)timeUntilRestart;
                WinText.gameObject.SetActive(true);
                scoreText.gameObject.SetActive(true);

                if (hasWon == true) {
                    roundNumber++;
                    Hashtable hash = new Hashtable();

                    if ((int)PV.Owner.CustomProperties["team"] == 0) {
                        WinText.text = "You WIN!";
                        hash.Add("score", (int)PV.Owner.CustomProperties["score"] + 2);
                    }
                    else if ((int)PV.Owner.CustomProperties["team"] == 1) {
                        WinText.text = "You LOST!";
                        hash.Add("score", (int)PV.Owner.CustomProperties["score"] + 1);
                    }

                    PV.Owner.SetCustomProperties(hash);
                    hasWon = false;     //so that it doesnt keep adding the score
                }

                scoreText.text = "Score:  " + (int)PV.Owner.CustomProperties["score"];

                if (PhotonNetwork.IsMasterClient) {
                    if (timeUntilRestart <= 0) {
                        StartNewRound();
                    }
                }
                else {
                    if (timeUntilRestart <= -2) {   //TODO: send this timeUntilRestart over the network to not cause delay
                        StartNewRound();            //this is a bad solution to this problem lol
                    }
                }
            }

            else if (time > 0f) {
                MatchTimerStart();
            }
        }
    }

    void StartNewRound() {
        time = (int)PV.Owner.CustomProperties["time"];
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
            Invoke("CheckForDenners", 2.0f);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }
        if (newPlayer != player) {
            CheckForDenners();
            Invoke("CheckForDenners", 2.0f);
        }
    }*/

    public void LeaveRoom() {
        PhotonNetwork.Disconnect();       //need to disconnect the player before we change scenes
        SceneManager.LoadScene(0);
    }

    void MatchTimerStart() {
        timeUntilRestart = 10f;
        hasWon = true;

        if (PhotonNetwork.IsMasterClient) {
            time -= Time.deltaTime;

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;
            ht.Remove("Time");
            ht.Add("Time", time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else {
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }

        if (WinText != null) {
            TimeText.text = ((int)(float)PhotonNetwork.CurrentRoom.CustomProperties["Time"]).ToString();
            WinText.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(false);
        }
    }
}
