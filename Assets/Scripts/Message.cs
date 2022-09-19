using UnityEngine;
using TMPro;
using Photon.Pun;

public class Message : MonoBehaviourPunCallbacks { 

    public static void message(string _message, int destroyAfter = 5) {
        GameObject m = Instantiate(Resources.Load("PhotonPrefabs/Message", typeof(GameObject))) as GameObject;
        m.GetComponentInChildren<TMP_Text>().text = _message;
        Destroy(m, destroyAfter);
    }

    public static void messageToAll(string _message, PhotonView PV, RpcTarget rpcTarget, int destroyAfter = 5) {
        PV.RPC("message", rpcTarget, _message, destroyAfter);
    }
}