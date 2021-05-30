using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public static float mouseSens = 50f;

    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject TopScore;

    [SerializeField] Slider slider;
    [SerializeField] Slider volumeSlider;

    [SerializeField] GameObject leaderBoard;

    [SerializeField] TMP_Text mouseSensText;
    [SerializeField] TMP_Text volumeText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text coolDownText;
    [SerializeField] TMP_Text serverHost;

    [SerializeField] TMP_Text fpsButtonText;
    [SerializeField] TMP_Text fpsDisplayText;

    [SerializeField] TMP_Text[] score;
    [SerializeField] TMP_Text[] playerName;

    public TMP_Text yourScore;
    public TMP_Text yourName;
    
    [SerializeField] TMP_Text topScore;
    [SerializeField] TMP_Text topName;

    [SerializeField] TMP_Dropdown dropdown; 

    [SerializeField] MenuManager menuManager;

    Player[] playerList;

    [SerializeField] Color32[] teamColour;

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
    public float volume { get; set; } 

    public List<InputKeys> itemKeys = new List<InputKeys>();
    public Dictionary<string, InputKeys> movementKeys = new Dictionary<string, InputKeys>();
    public Dictionary<string, InputKeys> otherKeys = new Dictionary<string, InputKeys>();

    /*StoredData sens = new StoredData("sensitivity", 50);
    StoredData vol = new StoredData("volume", 100);
    StoredData fps = new StoredData("fps", 0);
    StoredData quality = new StoredData("quality", 2);*/

    bool fpsCounter = true;

    void Awake() {
        //Singleton pattern
        if (GM == null) {
            //DontDestroyOnLoad(gameObject);
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

        itemKeys.Add(new InputKeys("item1key", "Alpha1"));
        itemKeys.Add(new InputKeys("item2key", "Alpha2"));

        otherKeys.Add("prevWeapon", new InputKeys("prevWeaponKey", "Q"));
        otherKeys.Add("console", new InputKeys("consoleKey", "BackQuote"));
        otherKeys.Add("scoreboard", new InputKeys("scoreboardKey", "Tab"));
        otherKeys.Add("hideUI", new InputKeys("hideUIKey", "H"));
        otherKeys.Add("escape", new InputKeys("escapeKey", "Escape"));
        otherKeys.Add("fire", new InputKeys("fireKey", "Mouse0"));


        //this code is for whenever things go the wrong way
        for (int i = 0; i < itemKeys.Count; i++) {
            PlayerPrefs.SetString(itemKeys[i].keyName, itemKeys[i].defaultKeyValue.ToString());
            itemKeys[i].key = (KeyCode)Enum.Parse(typeof(KeyCode), itemKeys[i].defaultKeyValue);
        }

        //getting player values
        /*sensitivity = sens.dataInt;
        mouseSensText.text = sensitivity.ToString();
        PlayerMovement.sensitivity = sensitivity;
        MovementNoNetworking.sensitivity = sensitivity;

        volume = vol.dataFloat;
        AudioListener.volume = volume/100;
        
        fpsCounter = !ToBool(fps.dataInt);
        ChangeFPSOn();

        ChangeQuality(quality.dataInt);*/

        sensitivity = PlayerPrefs.GetInt("sensitivity", 50);
        mouseSensText.text = PlayerPrefs.GetInt("sensitivity", 50).ToString();
        volume = PlayerPrefs.GetInt("volume", 100);

        PlayerMovement.sensitivity = sensitivity;
        MovementNoNetworking.sensitivity = sensitivity;
        AudioListener.volume = volume / 100;

    }

    private void Start() {
        slider.value = sensitivity;
        volumeSlider.value = volume;
        GetQualityNames();
        menuManager.OpenMenu("pause");

        if (PhotonNetwork.CurrentRoom != null) 
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        else roomNameText.text = SceneManager.GetActiveScene().name;

    }

    private void Update() {
        pauseMenu.SetActive(gameIsPaused);

        if (Input.GetKeyDown(otherKeys["escape"].key) && SceneManager.GetActiveScene().buildIndex != 0)
            gameIsPaused = !gameIsPaused;

        if (!gameIsPaused) Resume();
        else Pause();

        DisplayPlayerList();

        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            ShowTopPlayers();

        if (fpsCounter) {
            float fps = 1 / Time.unscaledDeltaTime;
            fpsDisplayText.text = (int)fps + " FPS";
        }
    }

    void GetQualityNames() {
        dropdown.value = QualitySettings.GetQualityLevel();
        dropdown.options.Clear();
        foreach(string option in QualitySettings.names)
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
    }

    public void ChangeQuality(int value) {
        QualitySettings.SetQualityLevel(value);
        //quality.ChangePrefs(value);
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
        if (Input.GetKeyDown(otherKeys["scoreboard"].key)) {
            leaderBoard.SetActive(true);
            serverHost.gameObject.SetActive(PhotonNetwork.IsMasterClient);

            coolDownText.text = "Score Cooldown: " + (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] / 6;
            roundText.text = "Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"];
            if((int)PhotonNetwork.CurrentRoom.CustomProperties["roundNumber"] >= 5) {
                roundText.text = "Final Round";
            }

            for (int i = 0; i < playerList.Length; i++) {
                Player player = playerList[i];
                playerName[i].text = player.NickName;
                score[i].text = ((int)player.CustomProperties["score"]).ToString();
                playerName[i].color = teamColour[(int)player.CustomProperties["team"]];
                score[i].color = teamColour[(int)player.CustomProperties["team"]];
            }
            for (int i = PhotonNetwork.PlayerList.Length; i < 6; i++) {
                playerName[i].text = "...";
                score[i].text = "...";
            }
        }
        if(Input.GetKeyUp(otherKeys["scoreboard"].key)) {
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
        topName.color = teamColour[(int)topPlayer.CustomProperties["team"]];
        topScore.color = teamColour[(int)topPlayer.CustomProperties["team"]];

        yourScore.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["score"]).ToString();

        TopScore.SetActive(!gameIsPaused);
    }

    public void Pause() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = gameIsPaused;
    }

    public void Resume() {
        menuManager.CloseAllMenus();
        menuManager.OpenMenu("pause");

        DebugController.showConsole = false;
        gameIsPaused = false;

        if (SceneManager.GetActiveScene().buildIndex != 0) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = gameIsPaused;
        }
    }

    public void LeaveRoom() {
        Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
        hash.Remove("score");
        hash.Add("score", 1);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        PhotonNetwork.Disconnect();       //need to disconnect the player before we change scenes
        SceneManager.LoadScene(0);
        gameIsPaused = false;
    }

    public void ChangeMouseSens(float newSens) {
        PlayerMovement.sensitivity = newSens;
        MovementNoNetworking.sensitivity = newSens;
        //sens.ChangePrefs((int)newSens);
        PlayerPrefs.SetInt("sensitivity", (int)newSens);
        mouseSensText.text = ((int)newSens).ToString();
    }

    public void ChangeVolume(float newVolume) {
        AudioListener.volume = newVolume/100;
        //vol.ChangePrefs((int)newVolume);
        PlayerPrefs.SetInt("volume", (int)newVolume);
        volumeText.text = ((int)newVolume).ToString();
    }

    public void ChangeFPSOn() {
        fpsCounter = !fpsCounter;
        fpsButtonText.text = fpsCounter.ToString();
        fpsDisplayText.gameObject.SetActive(fpsCounter);
    }

    bool ToBool(int value) {
        if (value == 0) return false;
        else return true;
    }
}

public class StoredData {
    public string dataName;
    public int dataInt;
    public float dataFloat;
    public string dataString;

    public StoredData(string _dataName, string _data) {
        dataString = _data;
        dataName = _dataName;
        PlayerPrefs.SetString(dataName, _data);
    }

    public StoredData(string _dataName, float _data) {
        dataFloat = _data;
        dataName = _dataName;
        PlayerPrefs.SetFloat(dataName, _data);
    }

    public StoredData(string _dataName, int _data) {
        dataInt = _data;
        dataName = _dataName;
        PlayerPrefs.SetInt(dataName, _data);
    }

    public void ChangePrefs(int _data) {
        dataInt = _data;
        PlayerPrefs.SetInt(dataName, _data);
    }

    public void ChangePrefs(string _data) {
        dataString = _data;
        PlayerPrefs.SetString(dataName, _data);
    }

    public void ChangePrefs(float _data) {
        dataFloat = _data;
        PlayerPrefs.SetFloat(dataName, _data);
    }

}
