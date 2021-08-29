using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    [SerializeField] private float offset = 2;
    [SerializeField] private float crouchOffset = 0.5f;
    private float regularOffset;

    [SerializeField] bool isRotation = false;

    private void Awake() {
        regularOffset = offset;
    }

    void Update() {
        if (player == null)
            return;

        ChangeOffset();
        if (!isRotation)
            transform.position = player.transform.position + new Vector3(0, offset, 0);

        else
            transform.rotation = Quaternion.Euler(new Vector3(player.transform.rotation.eulerAngles.x, 0, 0));
       
    }

    void ChangeOffset() {
        if (Input.GetKeyDown(GameManager.GM.movementKeys["slide"].key) || Input.GetKeyDown(GameManager.GM.movementKeys["crouch"].key))
            offset = crouchOffset;
        if (Input.GetKeyUp(GameManager.GM.movementKeys["slide"].key) || Input.GetKeyUp(GameManager.GM.movementKeys["crouch"].key))
            offset = regularOffset;
    }
}
