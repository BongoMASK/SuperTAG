using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Properties;

public class GameManager : MonoBehaviour
{
    #region Variables

    //Used for singleton
    public static GameManager instance;
    public bool gameIsPaused = false;

    [Header("Menu")]
    public GameObject pauseMenu;
    [SerializeField] MenuManager menuManager;

    [Header("UI")]
    // Tagfeed
    public Transform tagFeedList;

    // Score
    [SerializeField] GameObject TopScore;
    [SerializeField] GameObject leaderBoard;
    public TMP_Text yourScore;
    public TMP_Text yourName;

    [SerializeField] TMP_Text topScore;
    [SerializeField] TMP_Text topName;

    [SerializeField] TMP_Text[] score;
    [SerializeField] TMP_Text[] playerName;

    // Sliders
    [SerializeField] Slider slider;
    [SerializeField] Slider volumeSlider;
    [SerializeField] Slider bloomSlider;

    // Text
    [SerializeField] TMP_Text mouseSensText;
    [SerializeField] TMP_Text volumeText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text serverHost;
    [SerializeField] TMP_Text ping;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text coolDownText;
    [SerializeField] TMP_Text decorButtonText;

    // FPS
    [SerializeField] TMP_Text fpsButtonText;
    [SerializeField] TMP_Text fpsDisplayText;

    //Post Processing
    [SerializeField] Volume PPvolume;
    [SerializeField] TMP_Text bloomIteration;
    [SerializeField] TMP_Text bloomButtonText;
    Bloom bloomSettings;

    // Dropdown
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] GameObject decorations;

    Player[] playerList;
    [HideInInspector]
    public List<Transform> playerObjectList = new List<Transform>();

    public int sensitivity { get; private set; }
    public float volume { get; set; } 

    public List<InputKeys> itemKeys = new List<InputKeys>();
    public Dictionary<string, InputKeys> movementKeys = new Dictionary<string, InputKeys>();
    public Dictionary<string, InputKeys> otherKeys = new Dictionary<string, InputKeys>();

    //data to be stored as playerprefs
    StoredData sens = new StoredData();
    StoredData vol = new StoredData();
    StoredData fps = new StoredData();
    StoredData quality = new StoredData();
    StoredData bloom = new StoredData();
    StoredData decor = new StoredData();

    bool fpsCounter = true;
    float currentTime;

    public PlayerManager playerManager { get; private set; }
    
    #endregion

    void Awake() {
        #region Singleton
        if (instance == null) 
            instance = this;
        
        else if (instance != this) 
            Destroy(gameObject);
        #endregion

        PPvolume.profile.TryGet(out bloomSettings);

        #region Input Keys

        //settings keys
        movementKeys.Add("jump", new InputKeys("jumpKey", "Space"));
        movementKeys.Add("forward", new InputKeys("forwardKey", "W"));
        movementKeys.Add("backward", new InputKeys("backwardKey", "S"));
        movementKeys.Add("left", new InputKeys("leftKey", "A"));
        movementKeys.Add("right", new InputKeys("rightKey", "D"));
        movementKeys.Add("slide", new InputKeys("slideKey", "LeftShift"));

        itemKeys.Add(new InputKeys("item1key", "Alpha1"));
        itemKeys.Add(new InputKeys("item2key", "Alpha2"));

        otherKeys.Add("prevWeapon", new InputKeys("prevWeaponKey", "Q"));
        otherKeys.Add("console", new InputKeys("consoleKey", "BackQuote"));
        otherKeys.Add("scoreboard", new InputKeys("scoreboardKey", "Tab"));
        otherKeys.Add("hideUI", new InputKeys("hideUIKey", "End"));
        otherKeys.Add("escape", new InputKeys("escapeKey", "Escape"));
        otherKeys.Add("fire", new InputKeys("fireKey", "Mouse0"));
        otherKeys.Add("chat", new InputKeys("chatKey", "Y"));
        otherKeys.Add("enter", new InputKeys("enterKey", "Return"));

        #endregion

        if (SceneManager.GetActiveScene().name == "Tutorial")
            PhotonNetwork.OfflineMode = true;

        //this code is for whenever things go the wrong way
        for (int i = 0; i < itemKeys.Count; i++) {
            PlayerPrefs.SetString(itemKeys[i].keyName, itemKeys[i].defaultKeyValue.ToString());
            itemKeys[i].key = (KeyCode)Enum.Parse(typeof(KeyCode), itemKeys[i].defaultKeyValue);
        }

        //getting player values
        sensitivity = sens.GetData("sensitivity", 50);
        mouseSensText.text = sensitivity.ToString();
        PlayerMovement.sensitivity = sensitivity;
        MovementNoNetworking.sensitivity = sensitivity;

        volume = vol.GetData("volume", 100);
        AudioListener.volume = volume/100;
        
        fpsCounter = !ToBool(fps.GetData("fps", 0));
        ChangeFPSOn();

        ChangeQuality(quality.GetData("quality", 2));

        bloomSettings.active = ToBool(bloom.GetData("bloom", ToInt(bloomSettings.active)));
        decorations.SetActive(ToBool(decor.GetData("decor", ToInt(decorations.activeSelf))));

        currentTime = Time.time;
    }

    private void Start() {

        #region Text Assignment

        slider.value = sensitivity;
        volumeSlider.value = volume;
        decorButtonText.text = decorations.activeSelf.ToString();
        bloomButtonText.text = bloomSettings.active.ToString();
        bloomSettings.skipIterations.value = 0;
        bloomIteration.text = bloomSettings.skipIterations.value.ToString();

        GetQualityNames();
        menuManager.OpenMenu("pause");

        if (!PhotonNetwork.OfflineMode) {
            if (PhotonNetwork.CurrentRoom != null)
                roomNameText.text = PhotonNetwork.CurrentRoom.Name;
            else 
                roomNameText.text = SceneManager.GetActiveScene().name;

            coolDownText.text = "Score Cooldown: " + (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] / 5;
            roundText.text = "Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber];
        }

        #endregion

        if (SceneManager.GetActiveScene().name == "vry gg map")
            Message.message("as the server host, type 'set_impulseBall reloadTime 3' in the console to change aspects of the game.", 10);

        if (SceneManager.GetActiveScene().name == "WinScreen")
            Invoke("LeaveRoom", 30);

        if (!PhotonNetwork.IsMasterClient)
            BugCatcher.instance.Disconnect();

        // Let them choose their team, if not master client
        if (!PhotonNetwork.IsMasterClient) {
            Pause();
            menuManager.CloseAllMenus();
            menuManager.OpenMenu(PlayerProps.team);
        }

        GetPlayerManager();
    }

    private void Update() {
        #region Pause / Resume
        
        if (Input.GetKeyDown(otherKeys["escape"].key) && SceneManager.GetActiveScene().buildIndex != 0) 
            gameIsPaused = !gameIsPaused;
        
        // Pause / Resume
        if (!gameIsPaused) Resume();
        else Pause();
        
        #endregion

        DisplayPlayerList();

        ShowTopPlayers();

        GetFPS();

        UpdatePlayerObjectList();
    }

    #region Manager Functions

    public void Pause() {
        gameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = gameIsPaused;
        pauseMenu.SetActive(gameIsPaused);
    }

    public void Resume() {
        menuManager.CloseAllMenus();
        menuManager.OpenMenu("pause");

        DebugController.showConsole = false;
        gameIsPaused = false;
        pauseMenu.SetActive(gameIsPaused);

        if (SceneManager.GetActiveScene().buildIndex != 0) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = gameIsPaused;
        }
    }

    void GetFPS() {
        if (fpsCounter && Time.time - currentTime > 1) {
            float fps = 1 / Time.unscaledDeltaTime;
            fpsDisplayText.text = (int)fps + " FPS";
            currentTime = Time.time;
        }
    }

    bool ToBool(int value) {
        if (value == 0) return false;
        else return true;
    }

    int ToInt(bool b) {
        if (b) return 1;
        return 0;
    }

    #endregion

    #region Network Functions

    void ShowTopPlayers() {
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            return;

        Player topPlayer = playerList[0];
        if (playerList[0] == PhotonNetwork.LocalPlayer) {
            topPlayer = playerList[1];
        }

        topName.text = topPlayer.NickName;
        topScore.text = ((int)topPlayer.CustomProperties[PlayerProps.score]).ToString();
        topName.color = PlayerInfo.Instance.teamColours[(int)topPlayer.CustomProperties[PlayerProps.team]];
        topScore.color = PlayerInfo.Instance.teamColours[(int)topPlayer.CustomProperties[PlayerProps.team]];

        yourScore.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.score]).ToString();

        TopScore.SetActive(!gameIsPaused);
    }

    void SortPlayersByScore() {
        playerList = PhotonNetwork.PlayerList;
        for (int i = 0; i < playerList.Length; i++) {
            for (int j = i + 1; j < playerList.Length; j++) {
                if ((int)playerList[i].CustomProperties[PlayerProps.score] < (int)playerList[j].CustomProperties[PlayerProps.score]) {
                    Player a = playerList[i];
                    playerList[i] = playerList[j];
                    playerList[j] = a;
                }
            }
        }
    }

    // Show PlayerScore when you press tab
    private void DisplayPlayerList() {
        SortPlayersByScore();
        
        if(Input.GetKey(otherKeys["scoreboard"].key))
            ping.text = PhotonNetwork.GetPing() + "ms";

        if (Input.GetKeyDown(otherKeys["scoreboard"].key)) {
            leaderBoard.SetActive(true);
            serverHost.gameObject.SetActive(PhotonNetwork.IsMasterClient);

            coolDownText.text = "Score Cooldown: " + (int)(float)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.maxTime] / 5;
            
            roundText.text = "Round " + (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber];
            if ((int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.roundNumber] >= (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.rounds])
                roundText.text = "Final Round";

            for (int i = 0; i < playerList.Length; i++) {
                Player player = playerList[i];
                playerName[i].text = player.NickName;
                score[i].text = ((int)player.CustomProperties[PlayerProps.score]).ToString();
                playerName[i].color = PlayerInfo.Instance.teamColours[(int)player.CustomProperties[PlayerProps.team]];
                score[i].color = PlayerInfo.Instance.teamColours[(int)player.CustomProperties[PlayerProps.team]];
            }
            for (int i = PhotonNetwork.PlayerList.Length; i < 6; i++) {
                playerName[i].text = "...";
                score[i].text = "...";
            }
        }
        if (Input.GetKeyUp(otherKeys["scoreboard"].key)) {
            leaderBoard.SetActive(false);
        }
    }

    public void UpdatePlayerObjectList() {
        PlayerNetworking[] list = FindObjectsOfType<PlayerNetworking>();
        playerObjectList.Clear();

        for (int i = 0; i < list.Length; i++)
            playerObjectList.Add(list[i].transform);
    }

    #endregion

    #region Button Functions

    public void LeaveRoom() {
        Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
        hash.Remove(PlayerProps.score);
        hash.Add(PlayerProps.score, 1);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        PhotonNetwork.LeaveRoom();       //need to disconnect the player before we change scenes
        SceneManager.LoadScene(0);
        gameIsPaused = false;
    }

    public void ChangeMouseSens(float newSens) {
        PlayerMovement.sensitivity = newSens;
        MovementNoNetworking.sensitivity = newSens;
        sens.ChangePrefs((int)newSens);
        //PlayerPrefs.SetInt("sensitivity", (int)newSens);
        mouseSensText.text = ((int)newSens).ToString();
    }

    public void ChangeBloomSettings(float newBloom) {
        bloomSettings.skipIterations.value = (int)newBloom;
        bloomIteration.text = newBloom.ToString();
    }

    public void BloomOnOff() {
        bloomSettings.active = !bloomSettings.active;
        bloom.ChangePrefs(ToInt(bloomSettings.active));
        bloomButtonText.text = bloomSettings.active.ToString();
    }

    public void ShowDecorations() {
        if (decorations == null)
            return;

        decorations.SetActive(!decorations.activeSelf);
        decorButtonText.text = decorations.activeSelf.ToString();
        decor.ChangePrefs(ToInt(decorations.activeSelf));
    }

    public void ChangeVolume(float newVolume) {
        AudioListener.volume = newVolume / 100;
        vol.ChangePrefs((int)newVolume);
        volumeText.text = ((int)newVolume).ToString();
    }

    public void ChangeFPSOn() {
        fpsCounter = !fpsCounter;
        fpsButtonText.text = fpsCounter.ToString();
        fps.ChangePrefs(ToInt(fpsCounter));
        fpsDisplayText.gameObject.SetActive(fpsCounter);
    }

    void GetQualityNames() {
        dropdown.options.Clear();
        foreach (string option in QualitySettings.names)
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        dropdown.value = QualitySettings.GetQualityLevel();
    }

    public void ChangeQuality(int value) {
        QualitySettings.SetQualityLevel(value);
        quality.ChangePrefs(value);
    }

    void GetPlayerManager() {
        PlayerManager[] list = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager l in list) {
            if (l.PV.IsMine) {
                playerManager = l;
                return;
            }
        }
    }

    public void JoinGameAsPlayer() {
        if (playerManager == null) 
            GetPlayerManager();
        
        playerManager.CreateController();
        Resume();
    }

    public void JoinGameAsSpectator() {
        if(playerObjectList.Count < 1) {
            string message = "Cant spectate when there is no one to spectate";
            Debug.LogError(message);
            Message.message(message);
            return;
        }

        if (playerManager == null)
            GetPlayerManager();

        playerManager.CreateSpectator();
        Resume();
    }

    #endregion
}

public class StoredData {
    private string dataName;

    public string GetData(string _dataName, string _data) {
        dataName = _dataName;
        return PlayerPrefs.GetString(dataName, _data);
    }

    public float GetData(string _dataName, float _data) {
        dataName = _dataName;
        return PlayerPrefs.GetFloat(dataName, _data);
    }

    public int GetData(string _dataName, int _data) {
        dataName = _dataName;
        return PlayerPrefs.GetInt(dataName, _data);
    }

    public void ChangePrefs(int _data) {
        PlayerPrefs.SetInt(dataName, _data);
    }

    public void ChangePrefs(string _data) {
        PlayerPrefs.SetString(dataName, _data);
    }

    public void ChangePrefs(float _data) {
        PlayerPrefs.SetFloat(dataName, _data);
    }
}
