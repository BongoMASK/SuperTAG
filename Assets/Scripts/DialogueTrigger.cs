using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    DialogueManager dialogueManager;

    private void Start() {
        dialogueManager = FindObjectOfType<DialogueManager>();
        TriggerDialogue();
    }

    public void TriggerDialogue() {
        dialogueManager.StartDialogue(dialogue);
    }
}
