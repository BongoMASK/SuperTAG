using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    PhotonView PV;

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    void Start()
    {
        if(PV.IsMine) {
            CreateController();
        }
    }

    void CreateController() {
        Vector3 spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerContainer 1"), spawnPosition, Quaternion.identity);
    }
}
