using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomListItem : MonoBehaviour
{

    [SerializeField] TMP_Text text;

    public RoomInfo info;

    public void SetUp(RoomInfo _info) {
        info = _info;
        text.text = _info.Name + ", Players: " + info.PlayerCount;
    }

    private static string GetSceneNameByIndex(int buildIndex) {
        if (buildIndex > SceneManager.sceneCountInBuildSettings - 1) {
            Debug.LogErrorFormat("Incorrect buildIndex {0}!", buildIndex);
            return null;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        return sceneName;
    }

    public void OnClick() {
        Launcher.Instance.JoinRoom(info);
    }
}
