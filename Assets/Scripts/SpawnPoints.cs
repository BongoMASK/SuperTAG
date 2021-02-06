using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class SpawnPoints : MonoBehaviour {
    public Transform[] spawnPoints;
    public TMP_Text[] playerNames;
    public TMP_Text[] top3Names;
    public GameObject[] players;

    [SerializeField] TMP_Text rank;

    Player[] playerList;

    private void Start() {
        AssignNames();
    }

    private void Update() {
        for (int i = 0; i < playerList.Length; i++) {
            playerNames[i].text = playerList[i].NickName + ", " + (int)playerList[i].CustomProperties["score"];
        }
    }

    void AssignNames() {
        playerList = PhotonNetwork.PlayerList;
        for (int i = 0; i < playerList.Length; i++) {
            for (int j = i + 1; j < playerList.Length; j++) {
                if ((int)playerList[i].CustomProperties["score"] < (int)playerList[j].CustomProperties["score"]) {
                    Player a = playerList[i];
                    playerList[i] = playerList[j];
                    playerList[j] = a;
                }
            }
        }

        for (int i = 0; i < playerList.Length; i++) {
            players[i].SetActive(true);
            playerNames[i].text = playerList[i].NickName + ", " + (int)playerList[i].CustomProperties["score"];
            if(PhotonNetwork.LocalPlayer == playerList[i]) {
                rank.text = "Your RANK: " + (i + 1);
            }
            if (top3Names[i] != null) {
                top3Names[i].text = playerList[i].NickName;
            }
        }

        for (int i = playerList.Length; i < 6; i++) {
            players[i].SetActive(false);
        }
    }
}
