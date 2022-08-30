using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{
    #region Variables

    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_InputField playerNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text playerCount;

    //Room List Variables
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;

    //Player variables
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;

    //Other GameObjects
    [SerializeField] GameObject[] masterClientButtons;

    [SerializeField] GameObject roomManager;

    //setting player properties
    int time = 120;
    int dennerCount = 1;
    int rounds = 5;
    int mapCount = 1;
    int tagCountdown = 5;

    //Text fields for all the room functions
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text dennerCountText;
    [SerializeField] TMP_Text roundCountText;
    [SerializeField] TMP_Text tagCountdownText;
    [SerializeField] TMP_Text mapText;
    [SerializeField] TMP_Text versionText;
    [SerializeField] TMP_Text noRooms;

    [SerializeField] TMP_InputField roomNameField;

    private List<GameObject> _listings = new List<GameObject>();

    // string url = "https://b0ngo.itch.io/supertag";
    string url = "https://b0ngo.itch.io/supertag-test";

    #endregion

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        // If player has never played the game before, Open Tutorial
        int hasPlayed = PlayerPrefs.GetInt("hasPlayed", 0);
        if (hasPlayed == 0)
            StartTutorial();

        Connect();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Check if current version is latest version
        StartCoroutine(BugCatcher.instance.GetDataFromWebpage(url));
    }

    private void Update() {
        SetLauncherDetails();
    }

    void SetLauncherDetails() {
        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";
        dennerCountText.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["denner"]).ToString();

        if (PhotonNetwork.CurrentRoom != null && tagCountdownText.isActiveAndEnabled) {
            roundCountText.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties["rounds"]).ToString();
            tagCountdownText.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"]).ToString();
            mapText.text = GetSceneNameByIndex((int)PhotonNetwork.CurrentRoom.CustomProperties["mapCount"]);
        }

        playerCount.text = "Players Online: " + PhotonNetwork.CountOfPlayers;
    }

    void ErrorMessage(string message) {
        errorText.text = message;
        Debug.LogError(message);
        MenuManager.Instance.OpenMenu("error");
    }

    #region Photon Functions
    
    public void JoinRoom(RoomInfo info) {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    // Connect to master server
    void Connect() {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        PhotonNetwork.OfflineMode = false;

        Debug.Log("connecting to Master");
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = Application.version;
        versionText.text = "TEST v" + PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion;
        //versionText.text = "v" + PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion;
    }

    // Assigns player custom properties
    void AssignPlayerDetails() {
        Hashtable hash = new Hashtable {
            { "time", time },
            { "denner", dennerCount }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";
        dennerCountText.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["denner"]).ToString();

        Instantiate(roomManager);

        Hashtable hash1 = new Hashtable {
            { "team", 0 },
            { "TeamName", "Runner" },
            { "name", PhotonNetwork.LocalPlayer.NickName },
            { "score", 1 },
            { "countdown", false },
            { "mapCount", mapCount },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash1);

        PhotonNetwork.NickName = PlayerPrefs.GetString("playerName", "Player " + Random.Range(0, 1000).ToString("0000"));
        playerNameText.text = PhotonNetwork.NickName;
    }

    #endregion

    #region Photon Overrides

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {    //12:47

        foreach (RoomInfo info in roomList) {
            //removed from list
            if (info.Name.StartsWith("PRIVATE_"))
                continue;

            else if (info.PlayerCount > 6)
                continue;

            else if (info.RemovedFromList) {
                int index = _listings.FindIndex(a => a.GetComponent<RoomListItem>().info.Name == info.Name);
                if (index != -1) {   //indicates if index is found
                    Destroy(_listings[index].gameObject);
                    _listings.RemoveAt(index);
                }
            }

            //added to list
            else {
                for (int i = 0; i < roomList.Count; i++) {
                    if (roomList[i].RemovedFromList) {
                        continue;
                    }

                    else if (info.Name == roomList[i].Name) {     //if room already exists, delete previous room
                        int index = _listings.FindIndex(a => a.GetComponent<RoomListItem>().info.Name == roomList[i].Name);
                        if (index != -1) {
                            Destroy(_listings[index].gameObject);
                            _listings.RemoveAt(index);
                        }
                    }
                    GameObject listing = Instantiate(roomListItemPrefab, roomListContent);
                    if (listing != null) {
                        listing.GetComponent<RoomListItem>().SetUp(roomList[i]);
                        _listings.Add(listing);
                    }
                }
            }
        }
        noRooms.gameObject.SetActive(_listings.Count <= 0);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public override void OnLeftRoom() {
        MenuManager.Instance.OpenMenu("title");
        print("left room");
    }

    public override void OnJoinRoomFailed(short returnCode, string message) {
        errorText.text = "Room Join Failed. " + message;
        Debug.LogError("Room Join Failed. " + message);
        MenuManager.Instance.OpenMenu("error");
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        for (int i = 0; i < masterClientButtons.Length; i++) {
            masterClientButtons[i].SetActive(PhotonNetwork.IsMasterClient);
        }
        Debug.Log("Master Client switched");
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        errorText.text = "Room Creation Failed. " + message;
        Debug.LogError("Room Creation Failed. " + message);
        MenuManager.Instance.OpenMenu("error");
    }

    public override void OnJoinedRoom() {

        _listings.Clear();
        foreach (Transform trans in roomListContent) {
            Destroy(trans.gameObject);
        }

        if (PhotonNetwork.IsMasterClient) {
            Hashtable hash = new Hashtable {
                { "time", time },
                { "denner", dennerCount },
                { "mapCount", mapCount },
                { "tagCountdown", tagCountdown},
                { "roundNumber", 1 },
                { "rounds", 5 },
                { "hasStarted", false}
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        if (!PhotonNetwork.IsMasterClient) {
            mapCount = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapCount"];
        }

        MenuManager.Instance.OpenMenu("room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach (Transform child in playerListContent) {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Count(); i++) {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        for (int i = 0; i < masterClientButtons.Length; i++) {
            masterClientButtons[i].SetActive(PhotonNetwork.IsMasterClient);
        }

        print("joined room");

        //if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["hasStarted"])
        //    PhotonNetwork.LoadLevel(mapCount);
    }

    public override void OnConnectedToMaster() {
        Debug.Log("connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;

        AssignPlayerDetails();
    }

    public override void OnDisconnected(DisconnectCause cause) {
        Debug.Log(cause);
    }

    public override void OnJoinedLobby() {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("joined lobby");
        if (!string.IsNullOrEmpty(BugCatcher.instance.roomName)) {
            MenuManager.Instance.OpenMenu("loading");
            PhotonNetwork.JoinRoom(BugCatcher.instance.roomName);
            print("Joining game");
        }
    }

    #endregion

    #region Button Functions
    [SerializeField] Slider slider;

    public void Quit() {
        Application.Quit();
    }

    public void SetTime(int increment) {
        if (time <= 20 && increment < 0) {
            return;
        }
        if (time >= 300 && increment > 0) {
            return;
        }

        time += increment;
        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";

        Hashtable hash = new Hashtable {
            { "time", time }
        };

        foreach (Player player in PhotonNetwork.PlayerList) {
            player.SetCustomProperties(hash);
        }
    }

    public void SetDenner(int increment) {
        if (dennerCount <= 1 && increment < 0) {
            return;
        }
        if (dennerCount >= PhotonNetwork.CurrentRoom.PlayerCount - 1 && increment > 0) {
            return;
        }

        dennerCount += increment;
        dennerCountText.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["denner"]).ToString();

        Hashtable hash = new Hashtable {
            { "denner", dennerCount }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        foreach (Player player in PhotonNetwork.PlayerList) {
            player.SetCustomProperties(hash);
        }
    }

    public void SetRounds(int increment) {
        if (rounds <= 1 && increment < 0) {
            return;
        }

        rounds += increment;

        Hashtable hash = new Hashtable {
            { "rounds", rounds }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        roundCountText.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties["rounds"]).ToString();

        foreach (Player player in PhotonNetwork.PlayerList)
            player.SetCustomProperties(hash);
    }

    public void SetCountdown(int increment) {
        if (tagCountdown <= 2 && increment < 0) {
            return;
        }
        if (tagCountdown >= 7 && increment > 0) {
            return;
        }

        tagCountdown += increment;
        tagCountdownText.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"]).ToString();

        Hashtable hash = new Hashtable {
            { "tagCountdown", tagCountdown }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void ChooseMap(int increment) {
        if (mapCount <= 1 && increment < 0) {
            return;
        }
        if (mapCount >= SceneManager.sceneCountInBuildSettings - 3 && increment > 0) {
            return;
        }

        mapCount += increment;
        mapText.text = GetSceneNameByIndex(mapCount);

        Hashtable hash = new Hashtable {
            { "mapCount", mapCount }
        };

        foreach (Player player in PhotonNetwork.PlayerList) {
            player.SetCustomProperties(hash);
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public void CreatePlayerName() {
        if (string.IsNullOrEmpty(playerNameInputField.text)) {
            ErrorMessage("Username must exist");
            return;
        }
        else if (playerNameInputField.text[0].ToString() == " ") {
            ErrorMessage("Cannot start username with a Space");
            return;
        }
        else if (playerNameInputField.text.Length >= 50) {
            ErrorMessage("Username cannot go over 50 characters");
            return;
        }

        PlayerPrefs.SetString("playerName", playerNameInputField.text);
        PhotonNetwork.NickName = playerNameInputField.text;
        playerNameText.text = playerNameInputField.text;
    }

    public void StartPractice() {
        SceneManager.LoadScene("Practice");
    }

    public void StartTutorial() {
        PlayerPrefs.SetInt("hasPlayed", 1);
        SceneManager.LoadScene("Tutorial");
    }

    public void LeaveRoom() {
        print("leving room");
        PhotonNetwork.LeaveRoom();
        Hashtable hash = new Hashtable();
        hash.Add("score", 1);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        MenuManager.Instance.OpenMenu("loading");
    }

    public void StartRoom() {
        Debug.Log("Loading map");
        MenuManager.Instance.OpenMenu("loading");

        PhotonNetwork.LoadLevel(mapCount);
        if (PhotonNetwork.IsMasterClient) {
            Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
            hash.Remove("hasStarted");
            hash.Add("hasStarted", true);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }

    public void CreateRoom() {
        RoomOptions options = new RoomOptions();
        if (string.IsNullOrEmpty(roomNameInputField.text)) {
            return;
        }

        options.MaxPlayers = 6;
        options.BroadcastPropsChangeToAll = true;

        // TODO: make the players set the room settings offline or in game

        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom() {
        if (roomNameField.text == null)
            return;
        PhotonNetwork.JoinRoom(roomNameField.text);
        MenuManager.Instance.OpenMenu("loading");
    }

    private static string GetSceneNameByIndex(int buildIndex) {
        if (buildIndex > SceneManager.sceneCountInBuildSettings - 1) {
            Debug.LogErrorFormat("Incorrect buildIndex {0}!", buildIndex);
            return null;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        return sceneName;
    }

    #endregion
}

[System.Serializable]
public class Tip {

    [TextArea(3, 10)]
    public string[] tips; 

    public string GetRandomTip() {
        string currentTip;
        currentTip = tips[Random.Range(0, tips.Length - 1)];
        return currentTip;
    }
};
