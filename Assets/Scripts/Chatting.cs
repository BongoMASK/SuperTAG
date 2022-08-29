using Photon.Pun;
using UnityEngine;
using TMPro;
using System.Collections;

public class Chatting : MonoBehaviourPunCallbacks {
    [SerializeField] PhotonView PV;
    [SerializeField] TMP_Text chatListText;
    [SerializeField] TMP_InputField chatBox;
    [SerializeField] PlayerMovement playerMovement;

    [SerializeField] int maxChats = 5;

    bool isChatting = false;

    private void Start() {
        if (!PV.IsMine) {
            Destroy(chatBox.gameObject.transform.parent.gameObject);
            chatListText = GameObject.FindGameObjectWithTag("Chat").GetComponent<TMP_Text>();
        }
    }

    void Update() {
        if (!PV.IsMine)
            return;
        if (chatBox == null)
            return;

        if (GameManager.instance.gameIsPaused)
            return;

        if (Input.GetKey(GameManager.instance.otherKeys["chat"].key) && !isChatting) {
            // Open Chat
            isChatting = true;
            chatBox.gameObject.SetActive(true);
            chatBox.text = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            chatBox.ActivateInputField();
            playerMovement.lockInput = true;
        }

        if (Input.GetKeyDown(GameManager.instance.otherKeys["enter"].key)) {
            //Send Chat message
            if (chatBox.text == "" || chatBox.text.Length > 100)
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
        StartCoroutine(UpdateChat(message));
    }

    IEnumerator UpdateChat(string message) {
        chatListText.text += message;

        yield return new WaitForSeconds(10);
        chatListText.text = chatListText.text.Remove(0, message.Length);
    }
}
