using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BugCatcher : MonoBehaviourPunCallbacks
{

    public static BugCatcher instance;
    [HideInInspector]
    public string roomName { get; private set; }

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

        currentVersion = "SuperTAG TEST v" + Application.version;
        // currentVersion = "SuperTAG v" + Application.version;
        Debug.Log(currentVersion);
    }

    public void Disconnect() {
        if (roomName == PhotonNetwork.CurrentRoom.Name) {
            roomName = null;
            Debug.Log("not disconnecting");
            return;
        }

        roomName = PhotonNetwork.CurrentRoom.Name;
        Debug.Log("disconnected");
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene(0);
    }

    public IEnumerator GetDataFromWebpage(string url) {
        WWW webpage = new WWW(url);
        while (!webpage.isDone)
            yield return false;
        
        string data;
        
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