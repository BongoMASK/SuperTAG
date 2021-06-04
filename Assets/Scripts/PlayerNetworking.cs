using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerNetworking : MonoBehaviourPunCallbacks {

    [SerializeField] GameObject glasses;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject tagFeed;
    [SerializeField] Transform tagFeedList;

    //player colour material
    private Renderer renderer;
    [SerializeField] Material[] material;

    public TMP_Text InfoText;

    //items
    [SerializeField] Item[] items;
    int itemIndex;
    int previousItemIndex = -1;
    int prevWeapon = -1;

    PhotonView PV;

    public AudioManager audioManager;

    float countdown = 5f;
    [SerializeField] float countdownStart = 5f;

    private void Awake() {
        PV = GetComponent<PhotonView>();
        renderer = GetComponent<Renderer>();
    }

    // Start is called before the first frame update
    void Start() {
        if (PV.IsMine) {
            EquipItem(1);
            GetComponent<MeshRenderer>().enabled = false;
            glasses.GetComponent<MeshRenderer>().enabled = false;
        }
        else {
            renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString();

        countdownStart = (int)PhotonNetwork.CurrentRoom.CustomProperties["tagCountdown"];

        ChangeColour();
    }

    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine) return;

        if (audioManager == null) 
            audioManager = FindObjectOfType<AudioManager>();

        if (countdown > -0.5f) { //So that it doesnt keep doing the countdown to infinity
            countdown -= Time.deltaTime;
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\nTag Cooldown: " + (int)countdown;
            InfoText.gameObject.SetActive(countdown > 0);
        }

        if (canvas != null) canvas.SetActive(!GameManager.gameIsPaused);
    }

    void ChangeItem() {
        for (int i = 0; i < items.Length; i++) {
            if (Input.GetKeyDown(GameManager.GM.itemKeys[i].key)) {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) {
            if (itemIndex >= items.Length - 1) {
                EquipItem(0);
            }
            else {
                EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) {
            if (itemIndex <= 0) {
                EquipItem(items.Length - 1);
            }
            else {
                EquipItem(itemIndex - 1);
            }
        }

        //switch to prev weapon
        if (Input.GetKeyDown(GameManager.GM.otherKeys["prevWeapon"].key)) {
            EquipItem(prevWeapon);
        }

        if (Input.GetKey(GameManager.GM.otherKeys["fire"].key)) {
            items[itemIndex].Use();
        }

    }

    void EquipItem(int _index) {
        if (_index == previousItemIndex) {
            return;
        }
        prevWeapon = itemIndex;
        itemIndex = _index;

        if (items.Length > 0) {
            items[itemIndex].itemGameObject.SetActive(true);
        }

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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        if (!PV.IsMine && targetPlayer == PV.Owner) {
            if (changedProps.ContainsKey("itemIndex")) {
                EquipItem((int)changedProps["itemIndex"]);
            }
            if (changedProps.ContainsKey("team")) {
                renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
            }
        }

        if (PV.IsMine && targetPlayer == PhotonNetwork.LocalPlayer) {
            if (changedProps.ContainsKey("team")) {
                ChangeOnTeamsChange();
            }
        }
    }

    void ChangeOnTeamsChange() {
        countdown = countdownStart;
        renderer.sharedMaterial = material[(int)PV.Owner.CustomProperties["team"]];
        audioManager.Play("TagSound");

        if (InfoText != null) {
            InfoText.text = "You are now the " + PhotonNetwork.LocalPlayer.CustomProperties["TeamName"].ToString() + "\nTag Cooldown: " + (int)countdown;
            InfoText.gameObject.SetActive(true);
            ChangeColour();

            if (PV.Owner.CustomProperties["team"] != null) {
                GetComponent<TeamSetup>().isDennerText.text = PV.Owner.CustomProperties["TeamName"].ToString();
            }
        }
    }

    [SerializeField] TMP_Text[] colourTexts;
    [SerializeField] Color32[] teamColour;

    void ChangeColour() {
        for (int i = 0; i < colourTexts.Length; i++)
            colourTexts[i].color = teamColour[(int)PV.Owner.CustomProperties["team"]];

        GameManager.GM.yourName.color = teamColour[(int)PV.Owner.CustomProperties["team"]];
        GameManager.GM.yourScore.color = teamColour[(int)PV.Owner.CustomProperties["team"]];
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player") && countdown <= 0f) {
            if ((int)PV.Owner.CustomProperties["team"] == 1 &&
            (int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties["team"] == 0) {

                // calling function to master client because only the master client can change the custom properties of other players
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, 1);

                ChangeMyTeam(0);
                countdown = countdownStart;

                //SendFeedToAll(PhotonNetwork.LocalPlayer, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner);
                PV.RPC("SpawnTagFeed", RpcTarget.All, PhotonNetwork.LocalPlayer, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner);
            }
            else if ((int)PV.Owner.CustomProperties["team"] == 0 &&
                (int)PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner.CustomProperties["team"] == 1) {

                // calling function to master client because only the master client can change the custom properties of other players
                PV.RPC("RPC_SwitchPlayerTeam", RpcTarget.MasterClient, other.gameObject.GetComponent<PhotonView>().ViewID, 0);

                ChangeMyTeam(1);
                countdown = countdownStart;

                //SendFeedToAll(PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner, PhotonNetwork.LocalPlayer);
                PV.RPC("SpawnTagFeed", RpcTarget.All, PhotonView.Find(other.gameObject.GetComponent<PhotonView>().ViewID).Owner, PhotonNetwork.LocalPlayer);
            }
        }
    }

    [PunRPC]
    void RPC_SwitchPlayerTeam(int viewID, int team) {
        Hashtable hash = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };

        PhotonView.Find(viewID).Owner.SetCustomProperties(hash);
    }

    void SendFeedToAll(Player player1, Player player2) {
        PV.RPC("SpawnTagFeed", RpcTarget.All, player1, player2);
    }

    void ChangeMyTeam(int team) {
        Hashtable hash2 = new Hashtable {
            { "team", team },
            { "TeamName", PlayerInfo.Instance.allTeams[team] }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash2);
    }

    [PunRPC]
    void RPC_PlaySound(int viewID, string name) {
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.Play(name);
    }

    [PunRPC]
    void RPC_PauseSound(int viewID, string name) {
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.Pause(name);
    }

    [PunRPC]
    void SpawnTagFeed(Player player1, Player player2) {
        DisplayTagFeed(player1, player2);
    }

    [PunRPC]
    void RPC_GetSound(int viewID) {
        PhotonView.Find(viewID).gameObject.GetComponent<PlayerMovement>().playerAudio.PlayRandomFootstep();
    }

    void DisplayTagFeed(Player player1, Player player2) {
        GameObject t = Instantiate(tagFeed);

        if (tagFeedList != null) {  //checks if there is tagfeed
            t.transform.SetParent(tagFeedList);
            if (tagFeedList.childCount > 4) {
                Destroy(tagFeedList.GetChild(0));
            }
        }
        else {
            Debug.Log("no tagfeed lol. view ID" + PV.ViewID);
        }

        t.GetComponentInChildren<TMP_Text>().text = player1.NickName + " <#FFF>tagged<#FF0000> " + player2.NickName;
        Destroy(t, 7f);
    }
}
