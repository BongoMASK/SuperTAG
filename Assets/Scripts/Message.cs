using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Message 
{
    public GameObject messageBox;
    string message;

    public Message(string _message) {
        message = _message;
        messageBox.GetComponentInChildren<TMP_Text>().text = message;
    }
}
