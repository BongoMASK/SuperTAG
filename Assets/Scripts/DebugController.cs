using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{

    public static bool showConsole;

    string input;

    public static DebugCommand KILL_ALL;
    public static DebugCommand hello;
    public static DebugCommand<int> setGold;
    public static DebugCommand<string, int> set_impulseBall;

    public List<object> commandList;

    public void OnToggleDebug() {
        showConsole = !showConsole;
    }

    private void Awake() {
        KILL_ALL = new DebugCommand("kill_all", "Removes all heroes from scene.", "kill_all", () => {
            Debug.Log("All enemies killed");
        });

        hello = new DebugCommand("hello", "Types hello in console.", "hello", () => {
            Debug.Log("helo");
        });

        setGold = new DebugCommand<int>("set_gold", "Sets the amount of gold", "set_gold <gold amount>", (a) => {
            Debug.Log("set gold to " + a);
        }); 
        
        set_impulseBall = new DebugCommand<string, int>("set_impulseBall", "Sets the amount of gold", "set_impulseBall <variable name> <value>", (b, a) => {
            Debug.Log("Changed impulse ball's " + b + "to " + a);
        });

        commandList = new List<object> {
            KILL_ALL,
            hello,
            setGold
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

        GUI.Box(new Rect(0, y, Screen.width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        input = GUI.TextField(new Rect(10f, y+ 5f, Screen.width - 20f, 20f), input);   
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
            }
        }
    }
}
