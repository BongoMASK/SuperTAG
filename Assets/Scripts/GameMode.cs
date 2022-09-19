using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using Properties;

public abstract class GameMode : MonoBehaviourPunCallbacks {

    #region Variables

    [Header("Game Mode Details")]
    [SerializeField] public string gameModeName;

    [TextArea(3, 3)]
    [SerializeField] public string gameModeDescription;


    [Header("Assignables")]
    [SerializeField] protected PhotonView PV;
    [SerializeField] protected ClientInfoManager clientInfoManager;


    [Header("FallDown score loss")]
    [SerializeField] int fallDown = -2;

    protected float time;

    protected bool isWaiting { get => GameManager.instance.playerObjectList.Count <= 1; }
    protected bool isPaused = false;

    #endregion

    #region Abstract Functions

    /// <summary>
    /// Runs every frame
    /// </summary>
    public abstract void GameLogicLoop();

    /// <summary>
    /// Function is called when 2 players collide against one another
    /// </summary>
    /// <param name="p"></param>
    /// <param name="other"></param>
    public abstract void HandleTag(Player p, Player other);

    /// <summary>
    /// Function is called when player collides against something that is not a player
    /// </summary>
    /// <param name="p"></param>
    /// <param name="other"></param>
    public abstract void HandleCollision(Player p, GameObject other);

    #endregion

    #region Score Functions

    /// <summary>
    /// Sets score to all players
    /// </summary>
    /// <param name="runnerPoints"></param>
    /// <param name="dennerPoints"></param>
    protected void SetScore(int runnerPoints, int dennerPoints) {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            // give points to runner
            if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 0)
                AddScore(PhotonNetwork.PlayerList[i], runnerPoints);

            // give points to denner
            else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1)
                AddScore(PhotonNetwork.PlayerList[i], dennerPoints);
        }
    }

    void AddScore(Player player, int score) {
        Hashtable hash = new Hashtable();
        int currentScore = (int)player.CustomProperties[PlayerProps.score];
        hash.Add(PlayerProps.score, currentScore + score);
        player.SetCustomProperties(hash);

        PV.RPC("RPC_ScoreAdder", player, score);
    }

    public void PlayerFallDownScore(Player player) {
        AddScore(player, fallDown);
    }

    #endregion

    /// <summary>
    /// Keep checking if roundCounter has increased or not to ensure round change
    /// </summary>
    /// <returns></returns>
    protected IEnumerator RoomUpdateSettings(string key, int value) {

        while ((int)PhotonNetwork.CurrentRoom.CustomProperties[key] != value) {
            Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
            hash2.UpdateHashtable(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// Keep checking if roundCounter has increased or not to ensure round change
    /// </summary>
    /// <returns></returns>
    protected IEnumerator RoomUpdateSettings(string key, float value) {

        while ((float)PhotonNetwork.CurrentRoom.CustomProperties[key] != value) {
            Hashtable hash2 = PhotonNetwork.CurrentRoom.CustomProperties;
            hash2.UpdateHashtable(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash2);

            Debug.Log((float)PhotonNetwork.CurrentRoom.CustomProperties[key]);

            yield return new WaitForSeconds(1);
        }
    }

    #region Common Functions

    /// <summary>
    /// Changes team between players
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="team1"></param>
    /// <param name="team2"></param>
    protected void Tag(Player p1, Player p2, int team1, int team2) {
        if (!(bool)p1.CustomProperties[PlayerProps.canTag])
            return;

        if ((int)p1.CustomProperties[PlayerProps.team] == team1) {
            if ((int)p2.CustomProperties[PlayerProps.team] == team2) {

                StartCoroutine(TagCooldown(p1));
                StartCoroutine(TagCooldown(p2));

                ChangePlayerTeam(p1, team2);
                ChangePlayerTeam(p2, team1);

                // Spawns tag feed
                PV.RPC("SpawnTagFeed", RpcTarget.All, p1, p2, "");
            }
        }
    }

    /// <summary>
    /// Sets canTag property false for countDown seconds
    /// Makes it true again
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    IEnumerator TagCooldown(Player p) {
        Hashtable h = p.CustomProperties;
        h.UpdateHashtable(PlayerProps.canTag, false);
        p.SetCustomProperties(h);

        int t = (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.tagCountdown];
        yield return new WaitForSeconds(t);

        h = p.CustomProperties;
        h.UpdateHashtable(PlayerProps.canTag, true);
        p.SetCustomProperties(h);
    }

    /// <summary>
    /// Gives player ammo
    /// </summary>
    /// <param name="p"></param>
    /// <param name="a"></param>
    protected void GivePlayerAmmo(Player p, AmmoPickUp a) {
        Debug.Log(p.NickName);
        // Give player ammo

        a.Refill(6);
    }

    /// <summary>
    /// Changes the team property of the player
    /// </summary>
    /// <param name="p"></param>
    /// <param name="team"></param>
    protected void ChangePlayerTeam(Player p, int team) {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, team }
        };
        p.SetCustomProperties(hash);
    }

    /// <summary>
    /// Checks if there are more or less denners
    /// </summary>
    protected void CheckForDenners() {
        Invoke("CheckForDenners", 20);

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1 || !PhotonNetwork.IsMasterClient) return;

        int runner = 0, denner = 0;

        // Check runner and denner count
        foreach (Player player in PhotonNetwork.PlayerList) {
            if ((int)player.CustomProperties[PlayerProps.team] == 0)
                runner++;
            else if ((int)player.CustomProperties[PlayerProps.team] == 1)
                denner++;
        }

        //if not enough denner make random person denner
        if (denner < (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { PlayerProps.team, 1 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if no runner, make random person runner
        else if (runner <= 0) {
            int value = Random.Range(0, PhotonNetwork.PlayerList.Length);
            Hashtable hash2 = new Hashtable {
                { PlayerProps.team, 0 }
            };
            PhotonNetwork.PlayerList[value].SetCustomProperties(hash2);
        }

        //if dennerCount > required denners, make random person denner
        else if (denner > (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) {
            int surplus = 0;

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                if (surplus >= denner - (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { PlayerProps.team, 0 }
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
                if (surplus >= PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.denner]) return;

                else if ((int)PhotonNetwork.PlayerList[i].CustomProperties[PlayerProps.team] == 1) {
                    Hashtable hash2 = new Hashtable {
                        { PlayerProps.team, 1 }
                    };
                    PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
                    surplus++;
                }
            }
        }
    }

    #endregion

    #region Console Commands

    public void ChangeScoreValues(string name, int newScore) {
        if (!PhotonNetwork.IsMasterClient) return;

        PV.RPC("ChangeScoreValuesOnAll", RpcTarget.AllBuffered, name, newScore);
    }

    public void ChangeTime(int newTime) {
        if (!PhotonNetwork.IsMasterClient) return;
        time = newTime;
    }

    public void ChangeMaxRounds(int newMaxRounds) {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        hash.Remove(RoomProps.rounds);
        hash.Add(RoomProps.rounds, newMaxRounds);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void ChangeRoomSettings(string name, int value) {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(name)) {
            Debug.Log("no such Room setting exists");
            return;
        }

        StartCoroutine(RoomUpdateSettings(name, value));

        Message.message("Changed " + name + " to: " + (int)PhotonNetwork.CurrentRoom.CustomProperties[name]);
        Invoke("CheckForDenners", 1);
    }

    public void ChangeGameTime(int newTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        StartCoroutine(RoomUpdateSettings(RoomProps.maxTime, (float)newTime));
    }

    public void PauseMatch() {
        if (!PhotonNetwork.IsMasterClient)
            return;
        isPaused = !isPaused;
    }

    public void RestartRound() {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // StartNewRound();
    }

    public void RestartGame() {
        if (!PhotonNetwork.IsMasterClient) return;
        // StartNewRound();

        Hashtable hash = new Hashtable {
            { PlayerProps.score, 0 }
        };
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash);

        StartCoroutine(RoomUpdateSettings(RoomProps.roundNumber, 1));
    }

    #endregion

}
