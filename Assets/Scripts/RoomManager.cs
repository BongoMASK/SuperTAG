using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    private void Awake() {
        if(Instance) {
            Destroy(gameObject);
            return;
        }

        if(Instance == null) {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    void Start() {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnEnable() {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable() {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
        //if (scene.buildIndex < SceneManager.sceneCountInBuildSettings - 3 && scene.buildIndex >= 1) {     //this is the game scene
        if (scene.buildIndex == 1) {     //this is for testing
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
        }
    }
}
