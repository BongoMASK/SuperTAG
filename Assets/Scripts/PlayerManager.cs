using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    PhotonView PV;
    GameObject controller;

    Camera spectatorCam;

    GameObject[] players; 

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    private void Start() {
        CreateController();
    }

    /// <summary>
    /// Instantiates Player into the scene
    /// </summary>
    public void CreateController() {
        if (!PV.IsMine)
            return;

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

    public void CreateSpectator() {
        
    }

    public void Die() {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }

    // TODO: Create Spectator Mode
    // TODO: Spawn player on button press
}
