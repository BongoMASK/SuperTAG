using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public static float mouseSens = 50f;

    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject optionsMenu;
    [SerializeField] GameObject mainPauseMenu;
    [SerializeField] Slider slider;

    [SerializeField] GameObject leaderBoard;

    [SerializeField] TMP_Text mouseSensText;
    [SerializeField] TMP_Text roomNameText;

    [SerializeField] TMP_Text[] score;
    [SerializeField] TMP_Text[] playerName;

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

    void Awake() {
        //Singleton pattern
        if (GM == null) {
            DontDestroyOnLoad(gameObject);
            GM = this;
        }
        else if (GM != this) {
            Destroy(gameObject);
        }

        /*Assign each keycode when the game starts.
		 * Loads data from PlayerPrefs so if a user quits the game, 
		 * their bindings are loaded next time. Default values
		 * are assigned to each Keycode via the second parameter
		 * of the GetString() function
		 */
        jump = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("jumpKey", "Space"));
        forward = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("forwardKey", "W"));
        backward = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("backwardKey", "S"));
        left = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("leftKey", "A"));
        right = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("rightKey", "D"));
        crouch = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("crouchKey", "LeftShift"));

        sensitivity = PlayerPrefs.GetInt("sensitivity", 50);
        mouseSensText.text = PlayerPrefs.GetInt("sensitivity", 50).ToString();

        PlayerMovement.sensitivity = sensitivity;
        MovementNoNetworking.sensitivity = sensitivity;
    }

    private void Start() {
        pauseMenu.SetActive(false);
        slider.value = sensitivity;
        //mouseSensText.text = PlayerMovement.sensitivity.ToString();
        //mouseSensText.text = MovementNoNetworking.sensitivity.ToString();

        if (PhotonNetwork.CurrentRoom != null) {
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        }
        else {
            roomNameText.text = SceneManager.GetActiveScene().name;
        }
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            if(gameIsPaused) {
                Resume();
            }
            else {
                Pause();
            }
        }

        DisplayPlayerList();
    }

    void SortPlayersByScore() {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            for (int j = i + 1; j < PhotonNetwork.PlayerList.Length; j++) {
                if ((int)PhotonNetwork.PlayerList[i].CustomProperties["score"] > (int)PhotonNetwork.PlayerList[j].CustomProperties["score"]) {
                    Player a = PhotonNetwork.PlayerList[i];
                    PhotonNetwork.PlayerList[i] = PhotonNetwork.PlayerList[j];
                    PhotonNetwork.PlayerList[j] = a;
                }
            }
        }
    }

    private void DisplayPlayerList() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            leaderBoard.SetActive(true);

            //SortPlayersByScore();
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                Player player = PhotonNetwork.PlayerList[i];
                playerName[i].text = player.NickName + " (" + player.CustomProperties["TeamName"].ToString() + ")";
                score[i].text = ((int)player.CustomProperties["score"]).ToString();
            }
        }
        if(Input.GetKeyUp(KeyCode.Tab)) {
            leaderBoard.SetActive(false);
        }
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
}
