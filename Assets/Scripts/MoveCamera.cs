using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    [SerializeField] private float offset = 2;

    void Update() {
        transform.position = player.transform.position + new Vector3(0, offset, 0);
    }
}
