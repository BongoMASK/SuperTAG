using Photon.Pun;
using UnityEngine;
using Properties;

public class GameModeManager : MonoBehaviour
{
    [SerializeField] GameMode[] gameModes;

    public static GameMode gameMode;

    private void Awake() {
        //foreach (GameMode gameMode in gameModes) {
        //    gameMode.enabled = false;
        //}
        gameMode = gameModes[0];
        gameMode.enabled = true;
    }
}
