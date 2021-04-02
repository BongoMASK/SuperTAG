using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{

    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_InputField playerNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text playerNameText;

    //Room List Variables
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;

    //Player variables
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;

    //Other GameObjects
    [SerializeField] GameObject[] masterClientButtons;

    [SerializeField] GameObject roomManager;

    int time = 120;
    int dennerCount = 1;
    int mapCount = 1;
    int tagCountdown = 5;

    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text dennerCountText;
    [SerializeField] TMP_Text tagCountdownText;
    [SerializeField] TMP_Text mapText;

    private List<GameObject> _listings = new List<GameObject>();

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        PhotonNetwork.OfflineMode = false;

        Debug.Log("connecting to Master");
        PhotonNetwork.ConnectUsingSettings();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        playerNameText.text = PlayerPrefs.GetString("playerName", "Player " + Random.Range(0, 1000).ToString("0000"));
    }

    private void Update() {
        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";
        dennerCountText.text = ((int)PhotonNetwork.LocalPlayer.CustomProperties["denner"]).ToString();
        if (PhotonNetwork.CurrentRoom != null && tagCountdownText != null) {
            tagCountdownText.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"]).ToString();
            mapText.text = GetSceneNameByIndex((int)PhotonNetwork.CurrentRoom.CustomProperties["mapCount"]);
        }
    }

    public override void OnConnectedToMaster() {
        Debug.Log("connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby() {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("joined lobby");
    }

    public void CreateRoom() {
        RoomOptions options = new RoomOptions();
        if(string.IsNullOrEmpty(roomNameInputField.text)) {
            return;
        }

        options.MaxPlayers = 6;
        options.BroadcastPropsChangeToAll = true;

        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("loading");
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

    public override void OnJoinedRoom() {
        _listings.Clear();
        foreach (Transform trans in roomListContent) {
            Destroy(trans.gameObject);
        }

        Hashtable hash = new Hashtable {
            { "time", time },
            { "denner", dennerCount },
            { "mapCount", mapCount },
            { "tagCountdown", tagCountdown},
            { "roundNumber", 1 }
        };

        if (PhotonNetwork.IsMasterClient) {
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

    public void StartMap1() {
        PhotonNetwork.LoadLevel(1);
    }

    public void StartMap2() {
        PhotonNetwork.LoadLevel(2);
    }

    public void StartRoom() {
        PhotonNetwork.LoadLevel(mapCount);
    }

    public void StartTutorial() {
        SceneManager.LoadScene("Tutorial");
    }

    public void StartPractice() {
        SceneManager.LoadScene("Practice");
    }

    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom(RoomInfo info) {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom() {
        MenuManager.Instance.OpenMenu("title");
    }

    void ErrorMessage(string message) {
        errorText.text = message;
        Debug.LogError(message);
        MenuManager.Instance.OpenMenu("error");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {    //12:47
        foreach(RoomInfo info in roomList) {
            //removed from list
            if(info.RemovedFromList) {
                int index = _listings.FindIndex(a => a.GetComponent<RoomListItem>().info.Name == info.Name);
                if(index != -1) {   //indicates if index is found
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
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
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

        foreach(Player player in PhotonNetwork.PlayerList) {
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
        if (mapCount >= SceneManager.sceneCountInBuildSettings - 4 && increment > 0) {
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

    private static string GetSceneNameByIndex(int buildIndex) {
        if (buildIndex > SceneManager.sceneCountInBuildSettings - 1) {
            Debug.LogErrorFormat("Incorrect buildIndex {0}!", buildIndex);
            return null;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        return sceneName;
    }

    public void Quit() {
        Application.Quit();
    }
}
