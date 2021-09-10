using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BugCatcher : MonoBehaviourPunCallbacks
{

    public static BugCatcher instance;
    [HideInInspector]
    public string roomName;

    private string data;
    string currentVersion;

    private void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        currentVersion = "SuperTAG v" + Application.version;
        Debug.Log(currentVersion);

        //StartCoroutine(BugCatcher.instance.GetDataFromWebpage(url));
    }

    public void Disconnect() {
        if (roomName == PhotonNetwork.CurrentRoom.Name) {
            roomName = null;
            return;
        }

        roomName = PhotonNetwork.CurrentRoom.Name;
        PhotonNetwork.Disconnect();
        Debug.Log("disconnected");
        SceneManager.LoadScene(0);
    }

    public override void OnConnectedToMaster() {
        Debug.Log("connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby() {
        MenuManager.Instance.OpenMenu("title"); 
        Debug.Log("joined lobby");
        if (!string.IsNullOrEmpty(roomName)) {
            MenuManager.Instance.OpenMenu("loading");
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    public IEnumerator GetDataFromWebpage(string url) {
        WWW webpage = new WWW(url);
        while (!webpage.isDone)
            yield return false;

        data = webpage.text;
        if (data.Contains(currentVersion)) {
            Debug.Log("Version matched!");
            PhotonNetwork.ConnectUsingSettings();
        }

        else
            StartCoroutine(OpenBrowser(url));
    }

    IEnumerator OpenBrowser(string url) {
        MenuManager.Instance.OpenMenu("browser");
        yield return new WaitForSeconds(5);
        Application.OpenURL(url);
    }
}