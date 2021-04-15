using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputKeys
{
    public string keyName;
    public string defaultKeyValue;
    public KeyCode key;
    
    public InputKeys(string name, string value) {
        keyName = name;
        defaultKeyValue = value;
        key = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString(keyName, defaultKeyValue));
    }
}
