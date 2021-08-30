using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{

    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text dialogueText;

    private Queue<string> sentences;

    void Awake() {
        sentences = new Queue<string>();
        CheckForConditions check = new CheckForConditions();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Return))
            DisplayNextSentence();
    }

    public void StartDialogue(Dialogue dialogue) {
        nameText.text = dialogue.name;

        sentences.Clear();

        foreach(string sentence in dialogue.sentences) {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    void DisplayNextSentence() {
        if(sentences.Count == 0) {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence) {
        dialogueText.text = "";
        foreach(char letter in sentence.ToCharArray()) {
            dialogueText.text += letter;
            yield return null;
        }
    }

    void EndDialogue() {
        gameObject.SetActive(false);
    }
}

public class CheckForConditions : MonoBehaviour {

    public bool WASD = false;
    public bool jump = false;
    public bool slide = false;
    public bool crouch = false;
    public bool slideJump = false;

    public bool IsWASD() {
        bool w = false, a = false, s = false, d = false;
        if (Input.GetKey(GameManager.GM.movementKeys["right"].key))
            d = true;
        if (Input.GetKey(GameManager.GM.movementKeys["backward"].key))
            s = true;
        if (Input.GetKey(GameManager.GM.movementKeys["left"].key))
            a = true;
        if (Input.GetKey(GameManager.GM.movementKeys["forward"].key))
            w = true;

        WASD = w && a && s && d;
        return WASD;
    }

    public bool IsJump() {
        if (Input.GetKey(GameManager.GM.movementKeys["jump"].key))
            return jump = true;

        return jump = false;
    }

    public bool IsSlide() {
        if (Input.GetKey(GameManager.GM.movementKeys["slide"].key)) {
            Rigidbody rb = FindObjectOfType<Rigidbody>();

            if(rb.velocity.magnitude > 27)
                return slide = true;
        }

        return false;
    }

    public bool IsCrouched() {
        if (Input.GetKey(GameManager.GM.movementKeys["crouch"].key)) {
            Rigidbody rb = FindObjectOfType<Rigidbody>();

            if (rb.velocity.magnitude > 5)
                return crouch = true;
        }

        return false;
    }

    public bool IsSlideJump() {
        if (Input.GetKey(GameManager.GM.movementKeys["slide"].key) && Input.GetKey(GameManager.GM.movementKeys["jump"].key)) {
            Rigidbody rb = FindObjectOfType<Rigidbody>();

            if (rb.velocity.magnitude > 30)
                return slideJump = true;
        }

        return false;
    }

    public bool AllDone() {
        return WASD && jump && slide && crouch && slideJump;
    }
};
