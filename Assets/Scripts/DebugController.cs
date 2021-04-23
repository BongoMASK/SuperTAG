using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{

    public static bool showConsole;
    [SerializeField] GUIStyle consoleStyle;

    string input;

    public static DebugCommand<string, int> set_impulseBall;
    public static DebugCommand<string, int> set_goopGun;
    public static DebugCommand<string, int> set_player;
    public static DebugCommand<int> set_time;
    public static DebugCommand<float> set_game_time;
    public static DebugCommand time_pause;
    public static DebugCommand restart_round;
    public static DebugCommand restart_game;

    //TODO: set time, reset score, restart round

    public List<object> commandList;

    public void OnToggleDebug() {
        showConsole = !showConsole;
    }

    private void Awake() {
        set_impulseBall = new DebugCommand<string, int>("set_impulseBall", "Sets values of impulse ball", "set_impulseBall <variable name, value>", (b, a) => {
            Debug.Log("Changed impulse ball's " + b + " to " + a);
            ProjectileGun[] weapons = FindObjectsOfType<ProjectileGun>();
            foreach(ProjectileGun weapon in weapons) {
                weapon.ChangeValues(b, a, "Impulse Ball");
            }
        });

        set_goopGun = new DebugCommand<string, int>("set_goopGun", "Sets values of goop gun", "set_goopGun <variable name, value>", (b, a) => {
            Debug.Log("Changed goop gun's " + b + " to " + a);
            ProjectileGun[] weapons = FindObjectsOfType<ProjectileGun>();
            foreach (ProjectileGun weapon in weapons) {
                weapon.ChangeValues(b, a, "Goop Gun");
            }
        });

        set_player = new DebugCommand<string, int>("set_player", "Sets values of player", "set_player <variable name, value>", (b, a) => {
            Debug.Log("Changed player's " + b + " to " + a);
            PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
            foreach (PlayerMovement player in players) {
                player.ChangeValues(b, a);
            }
        });

        set_time = new DebugCommand<int>("set_time", "Sets values of player", "set_time <value>", (a) => {
            TeamSetup[] players = FindObjectsOfType<TeamSetup>();
            foreach (TeamSetup player in players) {
                player.ChangeTime(a);
            }
        });

        set_game_time = new DebugCommand<float>("set_game_time", "Sets values of player", "set_game_time <value>", (a) => {
            TeamSetup[] players = FindObjectsOfType<TeamSetup>();
            foreach (TeamSetup player in players) {
                player.ChangeGameTime(a);
            }
        });

        time_pause = new DebugCommand("time_pause", "Sets values of player", "time_pause <value>", () => {
            TeamSetup[] players = FindObjectsOfType<TeamSetup>();
            foreach (TeamSetup player in players) {
                player.PauseMatch();
            }
        });

        restart_round = new DebugCommand("restart_round", "Sets values of player", "restart_round", () => {
            TeamSetup[] players = FindObjectsOfType<TeamSetup>();
            foreach (TeamSetup player in players) {
                player.RestartRound();
            }
        });

        restart_game = new DebugCommand("restart_game", "Sets values of player", "restart_game", () => {
            TeamSetup[] players = FindObjectsOfType<TeamSetup>();
            foreach (TeamSetup player in players) {
                player.RestartGame();
            }
        });

        commandList = new List<object> { 
            set_impulseBall,
            set_goopGun,
            set_player,
            set_time,
            set_game_time,
            time_pause,
            restart_round,
            restart_game
        };
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Return)) {
            if (showConsole) {
                HandleInput();
                input = "";
            }
        }
    }

    private void OnGUI() {
        if(!showConsole) return;

        float y = 0f;

        GUI.Box(new Rect(0, y, Screen.width, 60), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        input = GUI.TextField(new Rect(10f, y+ 5f, Screen.width - 20f, 100f), input, consoleStyle);   
        
    }

    void HandleInput() {
        string[] properties = input.Split(' ');

        for (int i = 0; i < commandList.Count; i++) {
            DebugCommandbase commandbase = commandList[i] as DebugCommandbase;
            if (input.Contains(commandbase.commandId)) {
                if (commandList[i] as DebugCommand != null) {
                    (commandList[i] as DebugCommand).Invoke();
                }
                else if(commandList[i] as DebugCommand<int> != null) {
                    (commandList[i] as DebugCommand<int>).Invoke(int.Parse(properties[properties.Length - 1]));
                }
                else if(commandList[i] as DebugCommand<string, int> != null) {
                    (commandList[i] as DebugCommand<string, int>).Invoke(properties[properties.Length - 2], int.Parse(properties[properties.Length - 1]));
                }
                goto l;
            }
        }
        Debug.LogWarning("no command like this lol");
        l:;
    }
}
