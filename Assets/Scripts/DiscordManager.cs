using System;
using UnityEngine;

public class DiscordManager : MonoBehaviour {

    public static DiscordManager instance;

    public Discord.Discord discord;

    public string state = "Main Menu";
    public string details = "";
    public string largeImage = "game_logo";
    public string smallImage = "team_";

    private void Awake() {
        if (instance != null) {
            Destroy(gameObject);
        }
        else {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        discord = new Discord.Discord(1014943311687057478, (System.UInt64)Discord.CreateFlags.Default);
    }

    void Update() {
        discord.RunCallbacks();
    }

    public void UpdateDiscord(long time = 0, int team = 2) {
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity {
            Details = details,
            State = state,
            Assets = {
                LargeImage = largeImage,
                LargeText = "SuperTAG for FREE on itch.io!",
                SmallImage = smallImage + team.ToString(),
                SmallText = PlayerInfo.Instance.allTeams[team],
            },
            Timestamps = {
                End = time,
            }
        };
        activityManager.UpdateActivity(activity, (res) => {
            if (res != Discord.Result.Ok)
                Debug.LogError("Discord Not Updated");
        });
    }

    private void OnApplicationQuit() {
        discord.Dispose();
        print("Disposed Discord");
    }
}