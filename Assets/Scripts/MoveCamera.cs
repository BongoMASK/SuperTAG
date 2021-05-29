using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    [SerializeField] private float offset = 2;

    [SerializeField] bool isRotation = false;

    void Update() {
        if (player != null) {
            if (!isRotation)
                transform.position = player.transform.position + new Vector3(0, offset, 0);

            else
                transform.rotation = Quaternion.Euler(new Vector3(player.transform.rotation.eulerAngles.x, 0, 0));
        }
    }
}
