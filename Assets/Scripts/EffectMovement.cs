using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectMovement : MonoBehaviour {
    public enum Axis {
        X, Y, Z
    }

    [SerializeField] Rigidbody rb;

    Vector3 startingPos;
    void Start() {
        startingPos = transform.position;
    }

    void Update() {

    }

    private void OnCollisionEnter(Collision collision) {
        rb.velocity *= 3;
    }
}
