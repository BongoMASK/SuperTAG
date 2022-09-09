using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Properties;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    Player player;

    public void Start() {
        Hashtable hash = new Hashtable {
            { PlayerProps.team, 2 },
            { PlayerProps.score, 1 },
        };
        player.SetCustomProperties(hash);
    }

    public void SetUp(Player _player) {
        player = _player;
        text.text = player.NickName;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        if(player == otherPlayer) {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom() {
        Destroy(gameObject);
    }
}
