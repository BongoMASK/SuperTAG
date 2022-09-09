using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Properties;
using System.Collections;

public class PlayerNetworking : MonoBehaviourPunCallbacks, AmmoInterface {

    #region Variables
    public PhotonView PV { get; private set; }

    [Header("Assignables")]

    //player colour material
    [SerializeField] Renderer rend;

    [SerializeField] Transform pointer;
    [SerializeField] Transform orientation;

    [SerializeField] GameObject canvas;
    [SerializeField] GameObject tagFeed;
    
    [Header("Text")]
    [SerializeField] TMP_Text InfoText;
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text[] colourTexts;

    [Header("Other")]
    [SerializeField] Item[] items;
    [SerializeField] float refillTime = 6f;

    // Items
    int itemIndex;
    int previousItemIndex = -1;
    int prevWeapon = -1;

    // Tagging
    int countdownStart {
        get => (int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.tagCountdown];
    }
    bool isTaggable = true;

    #endregion

    void Awake() {
        PV = GetComponent<PhotonView>();
    }

    void Start() {
        if (PV.IsMine) {
            EquipItem(1);
            GetComponent<MeshRenderer>().enabled = false;
            Destroy(playerNameText.gameObject);
        }
        else {
            Destroy(pointer);
            rend.sharedMaterial = PlayerInfo.Instance.teamMaterials[(int)PV.Owner.CustomProperties[PlayerProps.team]];
            playerNameText.text = PV.Owner.NickName;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ChangeColour();

        SendFeedToAll(PhotonNetwork.LocalPlayer, null, "joined the game");
    }

    void Update() {
        if (!PV.IsMine)
            return;

        FindClosestPlayer();

        if (!GameManager.instance.gameIsPaused)
            ChangeItem();

        if (canvas != null) 
            canvas.SetActive(!GameManager.instance.gameIsPaused);

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
                { PlayerProps.itemIndex, itemIndex }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    #endregion

    #region Photon Overrides

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        //when something happens to other players
        if (!PV.IsMine && targetPlayer == PV.Owner) {
            if (changedProps.ContainsKey(PlayerProps.itemIndex)) {
                EquipItem((int)changedProps[PlayerProps.itemIndex]);
            }
            if (changedProps.ContainsKey(PlayerProps.team)) {
                rend.sharedMaterial = PlayerInfo.Instance.teamMaterials[(int)PV.Owner.CustomProperties[PlayerProps.team]];
            }
        }

        //when something happens to you
        if (PV.IsMine && targetPlayer == PhotonNetwork.LocalPlayer) {
            if (changedProps.ContainsKey(PlayerProps.team)) {
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
            GameManager.instance.playerManager.OnFall(PhotonNetwork.LocalPlayer);
            SendFeedToAll(PhotonNetwork.LocalPlayer, null, "fell to a painful death");
        }
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

    /// <summary>
    /// Called when player custom properties change
    /// </summary>
    void ChangeOnTeamsChange() {
        StartCoroutine(TeamsChanged());
    }

    IEnumerator TeamsChanged() {
        isTaggable = false;
        rend.sharedMaterial = PlayerInfo.Instance.teamMaterials[(int)PV.Owner.CustomProperties[PlayerProps.team]];
        AudioManager.instance.Play("TagSound");
            
        ChangeColour();

        string denrun = "Chase after other Runners to Tag them";
        if ((int)PV.Owner.CustomProperties[PlayerProps.team] == 0)
            denrun = "Run from the Denner for as long as possible";

        if (InfoText != null) {
            InfoText.gameObject.SetActive(true);
            string team = PlayerInfo.Instance.allTeams[(int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProps.team]];
            InfoText.text = "You are now the " + team + "\n" + denrun + "\nTag Cooldown: " + countdownStart;
        }

        yield return new WaitForSeconds(countdownStart);

        InfoText.gameObject.SetActive(false);
        isTaggable = true;
    }

    void TagOtherPlayer(Collider other, int team1, int team2) {

        if ((int)PV.Owner.CustomProperties[PlayerProps.team] == team1) {
            if ((int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties[PlayerProps.team] == team2) {
                // calling function to master client because only the master client can change the custom properties of other players

                PhotonView PV = GameManager.instance.playerManager.PV;

                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, team1);
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, PV.ViewID, team2);

                isTaggable = false;

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
            colourTexts[i].color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];

        GameManager.instance.yourName.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];
        GameManager.instance.yourScore.color = PlayerInfo.Instance.teamColours[(int)PV.Owner.CustomProperties[PlayerProps.team]];
    }

    #endregion

    #region Remote Procedure Callbacks

    [PunRPC]
    void RPC_SwitchPlayerTeam(int viewID, int team) {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, team }
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
        if (!PV.IsMine)
            return;

        // Tagging
        if (other.gameObject.CompareTag("Player") && isTaggable) {
            TagOtherPlayer(other, 1, 0);
        }

        // Ammo Pickup
        if (other.gameObject.CompareTag("Ammo")) {
            GetAmmo(other.GetComponent<AmmoPickUp>());
        }
    }
}