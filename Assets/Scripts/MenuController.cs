using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MenuController : MonoBehaviour
{
    private ExitGames.Client.Photon.Hashtable _myCustomProperties = new ExitGames.Client.Photon.Hashtable();
    Player player;

    public void OnClickTeamSelect(int whichTeam) {
        if(PlayerInfo.Instance != null) {
            PlayerInfo.Instance.mySelectedTeam = whichTeam;
            PlayerPrefs.SetInt("MyTeam", whichTeam);
        }
    }

    public void OnClickSetTeam(int team) {
        SetTeam(team);
    }

    private void SetTeam(int team) {
        _myCustomProperties["team"] = team;
        _myCustomProperties["name"] = PhotonNetwork.NickName;
        
        if (team == 1) {
            _myCustomProperties["TeamName"] = "Denner";
        }
        else if (team == 0) {
            _myCustomProperties["TeamName"] = "Runner";
        }

        PhotonNetwork.SetPlayerCustomProperties(_myCustomProperties);
    }
}
