using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Properties;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientInfoManager : MonoBehaviourPunCallbacks
{
    #region Variables

    [Header("Assignables")]
    [SerializeField] PhotonView PV;
    [SerializeField] ServerInfoManager serverInfoManager;
    [SerializeField] PlayerManager playerManager;
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject scoreAdder;
    [SerializeField] Slider countDownSlider;

    [Header("TMP Components")]
    [SerializeField] TMP_Text isDennerText;
    [SerializeField] TMP_Text TimeText;
    [SerializeField] TMP_Text WinText;

    private int scoreParam = 1;
    private float time;
    float timer = -1;
    int currentRound {
        get => (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber];
    }

    public bool isWaiting {
        get => GameManager.instance.playerObjectList.Count <= 1;
    }

    bool winCheck = false;

    #endregion

    private void Start() {
        if (!PV.IsMine) {
            Destroy(canvas.gameObject);
            return;
        }

        // Setting Score CountDown Slider
        countDownSlider.maxValue = (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime];
        countDownSlider.minValue = countDownSlider.maxValue - serverInfoManager.scoreCountDown;

        WinText.gameObject.SetActive(false);
        ChangeTextColour();
    }

    private void Update() {
        if (!PV.IsMine)
            return;

        GameLogicLoop();
    }

    #region Time Functions

    void GameLogicLoop() {
        GetTime();
        SetText();
        PlayEndRoundSound();
        SetScoreCountdownSlider();
        if (time < 0)
            OnRoundEnd();
        else
            winCheck = false;
    }

    /// <summary>
    /// Sets the text in Player Manager UI
    /// </summary>
    void SetText() {
        // Setting Time text
        if (time > 0)
            TimeText.text = ((int)time).ToString();
        else
            TimeText.text = "\nRound " + currentRound + " starts in " + (10 + (int)time);

        // Waiting for Players
        if (isWaiting) {
            TimeText.text = "\nWaiting for Players";
            return;
        }

        WinText.gameObject.SetActive(time < 0);
    }

    /// <summary>
    /// Get time from room
    /// </summary>
    void GetTime() {
        time = (float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.Time];
    }

    /// <summary>
    /// Called only ONCE every time the round ends. 
    /// Its so that the winText does not change after the round is over
    /// Could be misleading to players
    /// </summary>
    void OnRoundEnd() {
        // So that WinText does not change mid-game
        if (winCheck)
            return;

        AudioManager.instance.Play("Round Timer End");

        // Set WinText
        if ((int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.team] == 0) {
            WinText.text = "You WIN!";
        }
        else if ((int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.team] == 1) {
            WinText.text = "You LOSE!";
        }
        winCheck = true;
    }

    /// <summary>
    /// Plays audio at the ending 10 seconds of the round
    /// </summary>
    void PlayEndRoundSound() {
        if (time > 10 || time < 0)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f) {
            timer = 1f;
            AudioManager.instance.Play("Round Timer");
        }
    }

    /// <summary>
    /// Change the Score Countdown Slider min and max values.
    /// The function body is executed every time the time exceeds the limits
    /// </summary>
    void SetScoreCountdownSlider() {
        if (time < countDownSlider.minValue || time > countDownSlider.maxValue) {
            countDownSlider.maxValue = time;
            countDownSlider.minValue = countDownSlider.maxValue - serverInfoManager.scoreCountDown;
        }
        countDownSlider.value = time > 0f && !isWaiting ? time : -100;
    }

    #endregion

    #region Client Functions

    /// <summary>
    /// Changes UI colour as per team
    /// </summary>
    void ChangeTextColour() {
        isDennerText.text = PlayerInfo.Instance.allTeams[(int)PV.Owner.CustomProperties[PlayerProps.team]];

        isDennerText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];
        TimeText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];
        WinText.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];
    }

    public void AddScore(int adder) {
        string add = "+";
        if (adder < 0)
            add = "-";

        GameObject s = Instantiate(scoreAdder);
        if (adder > scoreParam)
            s.GetComponentInChildren<TMP_Text>().color = new Color32(58, 117, 225, 255);

        s.GetComponentInChildren<TMP_Text>().text = add + Mathf.Abs(adder);
        Destroy(s, 1f);
    }

    #endregion

    #region Photon Overrides

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        if (!PV.IsMine)
            return;

        // Updates discord whenever there is a change in player properties
        if (targetPlayer == PhotonNetwork.LocalPlayer) {
            DiscordUpdate();
            
            // Change PlayerManager UI text colour
            if (changedProps.ContainsKey(PlayerProps.team))
                ChangeTextColour();
        }
    }

    #endregion

    /// <summary>
    /// Function for actually doing the DiscordUpdate
    /// </summary>
    void DoDiscordUpdate() {
        // Updating Discord Status
        string playing = "In Game - ";
        if (playerManager.isSpectator)
            playing = "Spectating - ";

        string players = " (" + GameManager.instance.playerObjectList.Count + " of 6)";

        string score = GetMaxScore() + " : " + (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.score];

        string state = playing + score + players;
        if (isWaiting)
            state = playing + "Waiting For Players";

        string details = "FFA - " + SceneManager.GetActiveScene().name + " (Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber] + ")";

        DateTimeOffset t1 = DateTimeOffset.Now;
        t1 = t1.AddSeconds((int)(time + 0.5));

        long t = isWaiting ? 0 : t1.ToUnixTimeSeconds();

        DiscordManager.instance.state = state;
        DiscordManager.instance.details = details;
        DiscordManager.instance.UpdateDiscord(t, (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.team]);
    }

    /// <summary>
    /// This is a different function coz I dont particularly enjoy using Invoke too much
    /// </summary>
    void DiscordUpdate() {
        Invoke("DoDiscordUpdate", 1);
    }

    /// <summary>
    /// Gets the highest score that is NOT the local players for the Discord score
    /// </summary>
    /// <returns></returns>
    int GetMaxScore() {
        int max = -999;

        foreach(Player p in PhotonNetwork.PlayerList) {
            if (p == PhotonNetwork.LocalPlayer)
                continue;
            int playerScore = (int)p.CustomProperties[PlayerProps.score];
            if (playerScore > max)
                max = playerScore;
        }

        return max;
    }
}