using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class TeamSetup : MonoBehaviour
{
    private PhotonView PV;

    public TMP_Text isDennerText;
    public TMP_Text TimeText;
    public TMP_Text WinText;
    public TMP_Text PlayerNameText;
    public TMP_Text scoreText;

    float time;
    float timeUntilRestart = 10f;
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

        if (PhotonNetwork.IsMasterClient) {
            time = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"];
            Hashtable ht = new Hashtable {
                { "Time", time }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
            Debug.Log("set time");
        }
        else {
            time = (float)PhotonNetwork.CurrentRoom.CustomProperties["Time"];
        }
    }

    void Update()
    {
        GameOver();
    }

    void GameOver() {
        if (time <= 0f && WinText != null) {        //after round finishes 
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

            timeUntilRestart -= Time.deltaTime;
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

    void StartNewRound() {
        time = (int)PV.Owner.CustomProperties["time"];
        Vector3 spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));
        transform.position = spawnPosition;
    }

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
