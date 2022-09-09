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

// Set team colours in playerInfo