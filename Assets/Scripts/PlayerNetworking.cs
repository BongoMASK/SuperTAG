using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class PlayerNetworking : MonoBehaviourPunCallbacks, AmmoInterface {

    #region Variables

    [SerializeField] GameObject canvas;
    [SerializeField] GameObject tagFeed;

    //player colour material
    private Renderer renderer;
    [SerializeField] Material[] material;

    [SerializeField] TMP_Text InfoText;

    //items
    [SerializeField] Item[] items;
    int itemIndex;
    int previousItemIndex = -1;
    int prevWeapon = -1;

    PhotonView PV;

    float countdown = 5f;
    [SerializeField] float countdownStart = 5f;

    [SerializeField] TMP_Text[] colourTexts;
    [SerializeField] Color32[] teamColour;

    [SerializeField] int fallDown = -3;

    [SerializeField] Transform pointer;
    [SerializeField] Transform orientation;

    [SerializeField] float refillTime = 6f;

    #endregion

    private void Awake() {
        if (SceneManager.GetActiveScene().name == "Tutorial")
            PhotonNetwork.OfflineMode = true;
        else
            PhotonNetwork.OfflineMode = false;

        PV = GetComponent<PhotonView>();
        renderer = GetComponent<Renderer>();
    }

    void Start() {
        if (PV.IsMine) {
            EquipItem(1);
            GetComponent<MeshRenderer>().enabled = false;
        }
        else {
            Destroy(pointer);
            renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString();

        countdownStart = (int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"];

        SetDenners();
        ChangeColour();

        SendFeedToAll(PhotonNetwork.LocalPlayer, null, "joined the game");
    }

    void Update() {
        if (!PV.IsMine)
            return;

        FindClosestPlayer();

        if (!GameManager.instance.gameIsPaused)
            ChangeItem();

        if (countdown > -0.5f) { //So that it doesnt keep doing the countdown to infinity
            countdown -= Time.deltaTime;
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\nTag Cooldown: " + (int)countdown;
            InfoText.gameObject.SetActive(countdown > 0);
        }

        if (canvas != null) canvas.SetActive(!GameManager.instance.gameIsPaused);

        Respawn();
    }

    #region Item Functions

    void ChangeItem() {
        for (int i = 0; i < items.Length; i++) {
            if (Input.GetKeyDown(GameManager.instance.itemKeys[i].key)) {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) {
            if (itemIndex >= items.Length - 1)
                EquipItem(0);

            else
                EquipItem(itemIndex + 1);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) {
            if (itemIndex <= 0)
                EquipItem(items.Length - 1);

            else
                EquipItem(itemIndex - 1);
        }

        //switch to prev weapon
        if (Input.GetKeyDown(GameManager.instance.otherKeys["prevWeapon"].key))
            EquipItem(prevWeapon);

        if (Input.GetKey(GameManager.instance.otherKeys["fire"].key))
            items[itemIndex].Use();
    }

    void EquipItem(int _index) {
        if (_index == previousItemIndex)
            return;

        prevWeapon = itemIndex;
        itemIndex = _index;

        if (items.Length > 0)
            items[itemIndex].itemGameObject.SetActive(true);

        if (previousItemIndex != -1 && items.Length > 0) {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (PV.IsMine) {
            Hashtable hash = new Hashtable {
                { "itemIndex", itemIndex }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    #endregion

    #region Photon Overrides

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        //when something happens to other players
        if (!PV.IsMine && targetPlayer == PV.Owner) {
            if (changedProps.ContainsKey("itemIndex")) {
                EquipItem((int)changedProps["itemIndex"]);
            }
            if (changedProps.ContainsKey("team")) {
                renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            }
        }

        //when something happens to you
        if (PV.IsMine && targetPlayer == PhotonNetwork.LocalPlayer) {
            if (changedProps.ContainsKey("team")) {
                ChangeOnTeamsChange();
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        if (!PV.IsMine) return;

        SpawnTagFeed(newPlayer, null, "is connecting");
    }

    public override void OnPlayerLeftRoom(Player player) {
        if (!PV.IsMine) return;

        SpawnTagFeed(player, null, "has disconnected");
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        if (!PV.IsMine) return;

        SpawnTagFeed(newMasterClient, null, "is the new Server Host");
    }

    #endregion

    #region Player Functions

    void Respawn() {    //when player falls off the edge of the map
        if (transform.position.y <= -40f) {
            transform.position = new Vector3(0f, 0f, 0f);
            PV.RPC("AddScore", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, fallDown);
            SendFeedToAll(PhotonNetwork.LocalPlayer, null, "fell to a painful death");
        }
    }

    void ChangeMyTeam(int team) {
        Hashtable hash2 = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash2);
    }

    void DisplayTagFeed(Player player1, Player player2 = null, string text = "") {
        GameObject t = Instantiate(tagFeed);

        t.transform.SetParent(GameManager.instance.tagFeedList);
        if (GameManager.instance.tagFeedList.childCount > 4) {
            Destroy(GameManager.instance.tagFeedList.GetChild(0).gameObject);
        }

        Destroy(t, 10f);
        if (player2 == null) {
            t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " <#FFF>" + text;
            return;
        }
        t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " <#FFF>tagged<#FF0000> " + player2.NickName;
    }

    public void GetAmmo(AmmoPickUp a) {
        Gun g = (Gun)items[(int)a.slot];
        if (g.currentAmmo == g.maxAmmo)
            return;
        a.Refill(refillTime);
        items[(int)a.slot].IncreaseAmmo();
    }

    void FindClosestPlayer() {
        Vector3 closest = GameManager.instance.playerObjectList[0].position;
        float minDist = 99999;
        foreach (Transform p in GameManager.instance.playerObjectList) {
            if (p == transform)
                continue;
            float dist = Vector3.Distance(transform.position, p.position);
            if (dist < minDist) {
                minDist = dist;
                closest = p.position;
            }
        }

        Vector3 vec = transform.position - closest;
        float angle = Vector3.SignedAngle(orientation.transform.forward, vec, Vector3.up);
        pointer.eulerAngles = new Vector3(0, 0, -angle);
    }

    #endregion

    #region Network Functions

    void ChangeOnTeamsChange() {
        countdown = countdownStart;
        renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
        AudioManager.instance.Play("TagSound");

        string denrun = "Chase after other Runners to Tag them";
        if ((int)PV.Owner.CustomProperties["team"] == 0)
            denrun = "Run from the Denner for as long as possible";

        if (InfoText != null) {
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\n" + denrun + "\nTag Cooldown: " + (int)countdown;
            InfoText.gameObject.SetActive(true);
            ChangeColour();

            if (PV.Owner.CustomProperties["team"] != null) {
                GetComponent<TeamSetup>().isDennerText.text = PV.Owner.CustomProperties["TeamName"].ToString();
            }
        }
    }

    void SetDenners() {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }

        int value = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount - (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]);

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount; i++) {
            Hashtable hash2 = new Hashtable {
                { "team", 0 },
                { "TeamName", PlayerInfo.Instance.allTeams[0] }
            };
            PhotonNetwork.PlayerList[i].SetCustomProperties(hash2);
        }

        for (int i = 0; i < (int)PhotonNetwork.CurrentRoom.CustomProperties["denner"]; i++) {
            Hashtable hash2 = new Hashtable {
                { "team", 1 },
                { "TeamName", PlayerInfo.Instance.allTeams[1] }
            };
            PhotonNetwork.PlayerList[value + i].SetCustomProperties(hash2);
        }
    }

    void TagOtherPlayer(Collider other, int team1, int team2) {
        //if (PV.ViewID == PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).ViewID)
        //  return;

        if ((int)PV.Owner.CustomProperties["team"] == team1) {
            if ((int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties["team"] == team2) {
                // calling function to master client because only the master client can change the custom properties of other players
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, team1);

                ChangeMyTeam(team2);
                countdown = countdownStart;

                SendFeedToAll(PhotonNetwork.LocalPlayer, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner);
            }
        }
    }

    void SendFeedToAll(Player player1, Player player2 = null, string text = "") {
        PV.RPC("SpawnTagFeed", RpcTarget.All, player1, player2, text);
    }

    public void ChangeRefillTime(int newValue) {
        if (!PhotonNetwork.IsMasterClient) return;

        PV.RPC("RPC_ChangeRefillTime", RpcTarget.AllBuffered, newValue);
    }

    //changes colour of texts as per team / den
    void ChangeColour() {
        for (int i = 0; i < colourTexts.Length; i++)
            colourTexts[i].color = teamColour[(int)PV.Owner.CustomProperties["team"]];

        GameManager.instance.yourName.color = teamColour[(int)PV.Owner.CustomProperties["team"]];
        GameManager.instance.yourScore.color = teamColour[(int)PV.Owner.CustomProperties["team"]];
    }

    #endregion

    #region Remote Procedure Callbacks

    [PunRPC]
    void RPC_SwitchPlayerTeam(int viewID, int team) {
        Hashtable hash = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };

        PhotonView.Find(viewID).Owner.SetCustomProperties(hash);
    }

    [PunRPC]
    void SpawnTagFeed(Player player1, Player player2 = null, string text = "") {
        DisplayTagFeed(player1, player2, text);
    }

    [PunRPC]
    void RPC_ChangeRefillTime(int newValue) {
        refillTime = newValue;

        Message.message("Changed refillTime to: " + newValue);
    }

    #endregion

    private void OnTriggerEnter(Collider other) {
        // Tagging
        if (other.gameObject.CompareTag("Player") && countdown <= 0f) {
            TagOtherPlayer(other, 1, 0);
        }

        // Ammo Pickup
        if (other.gameObject.CompareTag("Ammo")) {
            GetAmmo(other.GetComponent<AmmoPickUp>());
        }
    }
}
