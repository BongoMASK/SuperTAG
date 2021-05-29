using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class AssignKey : MonoBehaviour {

	[SerializeField] Transform[] optionPanel;   //put buttons and item keys here

	Event keyEvent;
	TMP_Text buttonText;
	KeyCode newKey;

	ChangedNames[] changedNames = {
		new ChangedNames("BackQuote", "`"),
		new ChangedNames("LeftControl", "L Ctrl"),
		new ChangedNames("LeftShift", "L Shift"),

	};
    readonly KeyCode[] MouseButtons = {
		KeyCode.Mouse0,
		KeyCode.Mouse1,
		KeyCode.Mouse2,
		KeyCode.Mouse3,
		KeyCode.Mouse4,
		KeyCode.Mouse5,
		KeyCode.Mouse6
	};

	bool waitingForKey;

	void Start() {

		waitingForKey = false;

		for (int j = 0; j < optionPanel.Length; j++) {
			for (int i = 0; i < optionPanel[j].childCount; i++) {

				GameObject option = optionPanel[j].GetChild(i).gameObject;

				if (GameManager.GM.movementKeys.ContainsKey(option.name)) {
					string name = ChangeButtonNames(GameManager.GM.movementKeys[option.name].key.ToString());
					option.GetComponentInChildren<TMP_Text>().text = name;
				}

				else if (GameManager.GM.otherKeys.ContainsKey(option.name)) {
					string name = ChangeButtonNames(GameManager.GM.otherKeys[option.name].key.ToString());
					option.GetComponentInChildren<TMP_Text>().text = name;
				}

				else if (keyNameExists(option.name)) {
					for (int k = 0; k < GameManager.GM.itemKeys.Count; k++)
						if (GameManager.GM.itemKeys[k].keyName == option.name)
							option.GetComponentInChildren<TMP_Text>().text = GameManager.GM.itemKeys[k].key.ToString();
				}

				else
					Debug.LogWarning(option.name + " does not exist.");
			}
		}
	}

	string ChangeButtonNames(string name) {
		for (int i = 0; i < changedNames.Length; i++)
			if (name == changedNames[i].unityName)
				return changedNames[i].myName;
		return name;
    }

	bool keyNameExists(string name) {
		for (int k = 0; k < GameManager.GM.itemKeys.Count; k++)
			if (GameManager.GM.itemKeys[k].keyName == name)
				return true;
		return false;
	}

	void OnGUI() {
		/*keyEvent dictates what key our user presses
		 * bt using Event.current to detect the current
		 * event
		 */
		keyEvent = Event.current;

		//Executes if a button gets pressed and
		//the user presses a key
		if (keyEvent.isKey && waitingForKey) {
			newKey = keyEvent.keyCode; //Assigns newKey to the key user presses
			waitingForKey = false;
		}

		if (keyEvent.isMouse && waitingForKey) {
			newKey = MouseButtons[keyEvent.button]; //Assigns newKey to the mouse user presses
			waitingForKey = false;
		}
	}

	/*Buttons cannot call on Coroutines via OnClick().
	 * Instead, we have it call StartAssignment, which will
	 * call a coroutine in this script instead, only if we
	 * are not already waiting for a key to be pressed.
	 */
	public void StartAssignment(string keyName) {
		if (!waitingForKey)
			StartCoroutine(AssignTheKey(keyName));
	}

	//Assigns buttonText to the text component of
	//the button that was pressed
	public void SendText(TMP_Text text) {
		buttonText = text;
	}

	//Used for controlling the flow of our below Coroutine
	IEnumerator WaitForKey() {
		while (waitingForKey) 
			yield return null;
	}

	/*AssignKey takes a keyName as a parameter. The
	 * keyName is checked in a switch statement. Each
	 * case assigns the command that keyName represents
	 * to the new key that the user presses, which is grabbed
	 * in the OnGUI() function, above.
	 */
	public IEnumerator AssignTheKey(string keyName) {
		waitingForKey = true;
		buttonText.text = "key...";

		yield return WaitForKey(); //Executes endlessly until user presses a key
		InputKeys inputKeys;

		foreach (var i in GameManager.GM.movementKeys) {
			if (keyName == i.Key) {
				inputKeys = i.Value;
				Assign(inputKeys);
			}
		}

		//make an array of input keys to make this better
		foreach (var i in GameManager.GM.otherKeys) {
			if (keyName == i.Key) {
				inputKeys = i.Value;
				Assign(inputKeys);
			}
		}

		for (int i = 0; i < GameManager.GM.itemKeys.Count; i++) {
			if(GameManager.GM.itemKeys[i].keyName == keyName) {
				inputKeys = GameManager.GM.itemKeys[i];
				Assign(inputKeys);
			}
		}

		yield return null;
	}

	void Assign(InputKeys inputKeys) {
		inputKeys.key = newKey;
		string name = ChangeButtonNames(inputKeys.key.ToString());
		buttonText.text = name;
		PlayerPrefs.SetString(inputKeys.keyName, inputKeys.key.ToString());
		GameObject myEventSystem = GameObject.Find("EventSystem");
		myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
	}
}

class ChangedNames {
	public string unityName;
	public string myName;

	public ChangedNames(string _unityName, string _myName) {
		unityName = _unityName;
		myName = _myName;
    }
}