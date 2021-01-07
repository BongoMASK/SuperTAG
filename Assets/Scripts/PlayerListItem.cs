using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    Player player;

    float time;

    public void Start() {
        Hashtable hash = new Hashtable {
            { "team", 0 },
            { "TeamName", "Runner" },
            { "name", PhotonNetwork.LocalPlayer.NickName },
            { "score", 1 },
            { "countdown", false }
        };
        player.SetCustomProperties(hash);
    }

    private void Update() {
        if (PhotonNetwork.IsMasterClient) {
            time = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"];
            Hashtable ht = new Hashtable {
                { "Time", time }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
    }

    public void SetUp(Player _player) {
        player = _player;
        text.text = player.NickName;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        if(player == otherPlayer) {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom() {
        Destroy(gameObject);
    }
}
