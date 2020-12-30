using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    public int mySelectedTeam;    // 1 for denner, 0 for runner, 2 for spectator

    public string[] allTeams;

    private void OnEnable() {
        if(PlayerInfo.Instance == null) {
            PlayerInfo.Instance = this;
        }
        else {
            if(PlayerInfo.Instance != this) {
                Destroy(PlayerInfo.Instance.gameObject);
                PlayerInfo.Instance = this;
            }
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        if(PlayerPrefs.HasKey("MyTeam")) {
            mySelectedTeam = PlayerPrefs.GetInt("MyTeam");
        }
        else {
            mySelectedTeam = 0;
            PlayerPrefs.SetInt("MyTeam", mySelectedTeam);
        }
    }

}
