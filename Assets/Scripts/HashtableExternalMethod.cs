using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class HashtableExternalMethod
{
    public static void UpdateHashtable(this Hashtable h, byte key, object value) {
        if (h.ContainsKey(key))
            h.Remove(key);
        h.Add(key, value);
    }

    public static void UpdateHashtable(this Hashtable h, object key, object value) {
        if (h.ContainsKey(key))
            h.Remove(key);
        h.Add(key, value);
    }
}
