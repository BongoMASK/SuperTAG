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
        SetPlayerText(_player);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        if(player == otherPlayer) {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom() {
        Destroy(gameObject);
    }

    private void SetPlayerText(Player _player) {
        string dennerOrNot;
        if (_player.CustomProperties.ContainsKey("team")) {
            dennerOrNot = _player.CustomProperties["TeamName"].ToString();
        }
        else {
            //Resetting custom properties here makes the client a master client for some reason
            dennerOrNot = "No Team";
        }
        text.text = player.NickName + " (" + dennerOrNot + ")";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        if(targetPlayer != null && targetPlayer == player) {
            if(changedProps.ContainsKey("team")) {
                SetPlayerText(targetPlayer);
            }
        }
    }

}
