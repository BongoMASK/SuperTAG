using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    public string[] allTeams;

    public Color32[] teamColours;

    public Material[] teamMaterials;

    private void Start() {
        if(Instance == null) {
            Instance = this;
        }
        else {
            if(Instance != this) {
                Destroy(Instance.gameObject);
                Instance = this;
            }
        }
        DontDestroyOnLoad(gameObject);
    }
}

// 0 - Runner
// 1 - Denner
// 2 - Spectator