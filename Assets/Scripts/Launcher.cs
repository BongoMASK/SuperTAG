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
    public string playerNameText;

    //Room List Variables
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;

    //Player variables
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;

    //Other GameObjects
    [SerializeField] GameObject startGameButton;
    [SerializeField] GameObject incrementTimeButton;
    [SerializeField] GameObject decrementTimeButton;
    [SerializeField] GameObject roomManager;

    int time = 120;
    [SerializeField] TMP_Text timeText;

    Player player;

    string randomName;

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("connecting to Master");
        PhotonNetwork.ConnectUsingSettings();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Hashtable hash = new Hashtable {
            { "time", time }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";

        Instantiate(roomManager);

        Hashtable hash1 = new Hashtable {
            { "team", 0 },
            { "TeamName", "Runner" },
            { "name", PhotonNetwork.LocalPlayer.NickName },
            { "score", 1 },
            { "countdown", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash1);
    }

    private void Update() {
        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";
    }

    public override void OnConnectedToMaster() {
        Debug.Log("connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby() {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("joined lobby");

        if (string.IsNullOrEmpty(playerNameInputField.text)) {
            randomName = "Player " + Random.Range(0, 1000).ToString("0000");
            PhotonNetwork.NickName = randomName;
        }
    }

    public void CreateRoom() {
        RoomOptions options = new RoomOptions();
        if(string.IsNullOrEmpty(roomNameInputField.text)) {
            return;
        }

        options.BroadcastPropsChangeToAll = true;

        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("loading");
    }

    public void CreatePlayerName() {
        if (string.IsNullOrEmpty(playerNameInputField.text)) {
            return;
        }
        playerNameText = playerNameInputField.text;
        PhotonNetwork.NickName = playerNameText;
    }

    public override void OnJoinedRoom() {
        MenuManager.Instance.OpenMenu("room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach (Transform child in playerListContent) {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Count(); i++) {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        incrementTimeButton.SetActive(PhotonNetwork.IsMasterClient);
        decrementTimeButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnJoinRoomFailed(short returnCode, string message) {
        errorText.text = "Room Join Failed. " + message;
        Debug.LogError("Room Join Failed. " + message);
        MenuManager.Instance.OpenMenu("error");
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
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

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        /*foreach (Transform trans in roomListContent) {
            Destroy(trans.gameObject);
        }*/

        for (int i = 0; i < roomList.Count; i++) {
            if(roomList[i].RemovedFromList) {
                continue;
            }
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public void SetTime(int increment) {
        time += increment;
        timeText.text = (int)PhotonNetwork.LocalPlayer.CustomProperties["time"] + "s";

        Hashtable hash = new Hashtable {
            { "time", time }
        };

        foreach(Player player in PhotonNetwork.PlayerList) {
            player.SetCustomProperties(hash);
        }
    }

    public void Quit() {
        Application.Quit();
    }
}
