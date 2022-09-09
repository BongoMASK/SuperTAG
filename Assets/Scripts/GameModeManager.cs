using Photon.Pun;
using UnityEngine;
using Properties;

public class GameModeManager : MonoBehaviour
{
    [SerializeField] GameMode[] gameModes;

    public static GameMode gameMode;

    public bool isRoundOver = true;

    private void Awake() {
        gameMode = gameModes[(int)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.mapCount]];
    }
}
