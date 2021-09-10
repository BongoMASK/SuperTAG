using Photon.Pun;
using UnityEngine;
using TMPro;

public class Chatting : MonoBehaviourPunCallbacks {
    [SerializeField] PhotonView PV;
    [SerializeField] TMP_Text chatListText;
    [SerializeField] TMP_InputField chatBox;
    [SerializeField] PlayerMovement playerMovement;

    [SerializeField] int maxChats = 5;

    int chatCount = 0;
    bool isChatting = false;

    private void Start() {
        if (!PV.IsMine) {
            Destroy(chatBox.gameObject.transform.parent.gameObject);
            chatListText = GameObject.FindGameObjectWithTag("Chat").GetComponent<TMP_Text>();
        }
    }

    void Update() {
        if (chatBox == null)
            return;

        if (Input.GetKey(GameManager.GM.otherKeys["chat"].key) && !isChatting) {
            // Open Chat
            isChatting = true;
            chatBox.gameObject.SetActive(true);
            chatBox.text = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            chatBox.ActivateInputField();
            playerMovement.lockInput = true;
        }

        if (Input.GetKeyDown(GameManager.GM.otherKeys["enter"].key)) {
            //Send Chat message
            if (chatBox.text == "" || chatBox.text.Length > 50)
                return;
            EnteredChat(chatBox.text);
        }
    }

    void EnteredChat(string message) {
        string chatMessage = "\n" + PhotonNetwork.LocalPlayer.NickName + ": <#FFF>" + message + "<#FFED00>";
        chatBox.DeactivateInputField();
        chatBox.text = null;
        chatBox.gameObject.SetActive(false);
        isChatting = false;
        playerMovement.lockInput = false;
        PV.RPC("SendChatToAll", RpcTarget.All, chatMessage);
    }

    [PunRPC]
    void SendChatToAll(string message) {
        UpdateChat(message);
    }

    void UpdateChat(string message) {
        chatListText.text += message;
        chatCount++;

        if (chatCount > maxChats)
            DeleteTillN();
    }

    void DeleteTillN() {
        int i;
        for (i = 0; i < chatListText.text.Length; i++)
            if (chatListText.text[i] == '\n')
                break;

        chatListText.text = chatListText.text.Remove(0, i + 1);
    }

}
