using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    PhotonView PV;

    GameObject controller;

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
        Vector3 spawnPosition;

        spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));

        if (FindObjectOfType<SpawnPoints>() != null) {
            Transform[] spawnPositions = FindObjectOfType<SpawnPoints>().spawnPoints;
            for (int i = 0; i < spawnPositions.Length; i++) {
                spawnPosition = spawnPositions[i].position;
            }
        }

        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerContainer 1"), spawnPosition, Quaternion.identity, 0, new object[] { PV.ViewID });
    }

    public void Die() {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }
}
