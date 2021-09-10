using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviour
{
    PhotonView PV;

    GameObject controller;
    //[SerializeField] GameObject canvas;

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    private void Start() {
        //if (PhotonNetwork.IsMasterClient)
            CreateController();
    }

    public void CreateController() {
        if (!PV.IsMine)
            return;

        //canvas.SetActive(false);
        Vector3 spawnPosition;

        spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));

        if (FindObjectOfType<SpawnPoints>() != null) {
            Transform[] spawnPositions = FindObjectOfType<SpawnPoints>().spawnPoints;
            for (int i = 0; i < spawnPositions.Length; i++) {
                spawnPosition = spawnPositions[i].position;
            }
        }

        if (controller != null)
            return;

        controller = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "PlayerContainer 1"), spawnPosition, Quaternion.identity, 0, new object[] { PV.ViewID });

    }

    public void Die() {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }
}
