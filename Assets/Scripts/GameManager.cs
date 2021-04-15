using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Realtime;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public static float mouseSens = 50f;

    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject TopScore;
    [SerializeField] GameObject optionsMenu;
    [SerializeField] GameObject mainPauseMenu;

    [SerializeField] Slider slider;
    [SerializeField] Slider volumeSlider;

    [SerializeField] GameObject leaderBoard;

    [SerializeField] TMP_Text mouseSensText;
    [SerializeField] TMP_Text volumeText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text coolDownText;

    [SerializeField] TMP_Text[] score;
    [SerializeField] TMP_Text[] playerName;

    [SerializeField] TMP_Text yourScore;
    [SerializeField] TMP_Text yourName;
    
    [SerializeField] TMP_Text topScore;
    [SerializeField] TMP_Text topName;

    Player[] playerList;

    //Used for singleton
    public static GameManager GM;

    //Create Keycodes that will be associated with each of our commands.
    //These can be accessed by any other script in our game
    public KeyCode jump { get; set; }
    public KeyCode forward { get; set; }
    public KeyCode backward { get; set; }
    public KeyCode left { get; set; }
    public KeyCode right { get; set; }
    public KeyCode crouch { get; set; }
    public int sensitivity { get; set; }
    public int volume { get; set; }

    public List<InputKeys> itemKeys = new List<InputKeys>();
    public Dictionary<string, InputKeys> movementKeys = new Dictionary<string, InputKeys>();

    void Awake() {
        //Singleton pattern
        if (GM == null) {
            DontDestroyOnLoad(gameObject);
            GM = this;
        }
        else if (GM != this) {
            Destroy(gameObject);
        }

        movementKeys.Add("jump", new InputKeys("jumpKey", "Space"));
        movementKeys.Add("forward", new InputKeys("forwardKey", "W"));
        movementKeys.Add("backward", new InputKeys("backwardKey", "S"));
        movementKeys.Add("left", new InputKeys("leftKey", "A"));
        movementKeys.Add("right", new InputKeys("rightKey", "D"));
        movementKeys.Add("crouch", new InputKeys("crouchKey", "LeftShift"));
        movementKeys.Add("prevWeapon", new InputKeys("prevWeaponKey", "Q"));

        itemKeys.Add(new InputKeys("item1key", "Alpha1"));
        itemKeys.Add(new InputKeys("item2key", "Alpha2"));

        //this code is for whenever things go the wrong way
        /*for (int i = 0; i < itemKeys.Count; i++) {
            PlayerPrefs.SetString(itemKeys[i].keyName, itemKeys[i].defaultKeyValue.ToString());
            itemKeys[i].key = (KeyCode)Enum.Parse(typeof(KeyCode), itemKeys[i].defaultKeyValue);
        }*/

        sensitivity = PlayerPrefs.GetInt("sensitivity", 50);
        mouseSensText.text = PlayerPrefs.GetInt("sensitivity", 50).ToString();
        volume = PlayerPrefs.GetInt("volume", 100);

        PlayerMovement.sensitivity = sensitivity;
        MovementNoNetworking.sensitivity = sensitivity;
        AudioListener.volume = volume/100;
    }

    private void Start() {
        mainPauseMenu.SetActive(false);
        slider.value = sensitivity;
        volumeSlider.value = volume;

        if (PhotonNetwork.CurrentRoom != null) {
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        }
        else {
            roomNameText.text = SceneManager.GetActiveScene().name;
        }
    }

    private void Update() {
        pauseMenu.SetActive(gameIsPaused);
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if(gameIsPaused) {
                Resume();
            }
            else {
                Pause();
            }
        }
        DisplayPlayerList();

        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1) {
            ShowTopPlayers();
        }
    }


    void SortPlayersByScore() {
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
    }

    private void DisplayPlayerList() {
        SortPlayersByScore();
        if (Input.GetKeyDown(KeyCode.Tab)) {
            leaderBoard.SetActive(true);

            coolDownText.text = "Score Cooldown: " + (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6;
            roundText.text = "Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"];
            if((int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] >= 5) {
                roundText.text = "Final Round";
            }

            for (int i = 0; i < playerList.Length; i++) {
                Player player = playerList[i];
                playerName[i].text = player.NickName + " (" + player.CustomProperties["TeamName"].ToString() + ")";
                score[i].text = ((int)player.CustomProperties["score"]).ToString();
            }
            for (int i = PhotonNetwork.PlayerList.Length; i < 6; i++) {
                playerName[i].text = "...";
                score[i].text = "...";
            }
        }
        if(Input.GetKeyUp(KeyCode.Tab)) {
            leaderBoard.SetActive(false);
        }
    }

    void ShowTopPlayers() {
        Player topPlayer = playerList[0];
        if(playerList[0] == PhotonNetwork.LocalPlayer) {
            topPlayer = playerList[1];
        }

        topName.text = topPlayer.NickName;
        topScore.text = ((int)topPlayer.CustomProperties["score"]).ToString();

        yourScore.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["score"]).ToString();

        TopScore.SetActive(!gameIsPaused);
    }

    public void Pause() {
        pauseMenu.SetActive(true);
        optionsMenu.SetActive(false);
        mainPauseMenu.SetActive(true);
        gameIsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume() {
        pauseMenu.SetActive(false);
        gameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LeaveRoom() {
        PhotonNetwork.Disconnect();       //need to disconnect the player before we change scenes
        SceneManager.LoadScene(0);
        gameIsPaused = false;
    }

    public void Options() {
        optionsMenu.SetActive(true);
        mainPauseMenu.SetActive(false);
    }

    public void ChangeMouseSens(float newSens) {
        PlayerMovement.sensitivity = newSens;
        MovementNoNetworking.sensitivity = newSens;
        PlayerPrefs.SetInt("sensitivity", (int)newSens);
        mouseSensText.text = ((int)newSens).ToString();
    }

    public void ChangeVolume(float newVolume) {
        AudioListener.volume = newVolume/100;
        PlayerPrefs.SetInt("volume", (int)newVolume);
        volumeText.text = ((int)newVolume).ToString();
    }
}
