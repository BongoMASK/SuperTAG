using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class RoomExtensionMethod
{
    //public static void UpdateCustomProperties(this Room r, string key, object value) {
    //   // StartCoroutine(CustomPropertiesUpdate(r, key, value));
    //}

    static IEnumerator CustomPropertiesUpdate(Room r, string key, object value) {
        while (r.CustomProperties[key] != value) {
            Hashtable hash = r.CustomProperties;
            hash.UpdateHashtable(key, value);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

            Debug.Log((int)PhotonNetwork.CurrentRoom.CustomProperties[key]);
            yield return new WaitForSeconds(1);
        }
    }

    static void prop(Room r, string key, object value) {

    }
}
