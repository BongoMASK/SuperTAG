using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectMovement : MonoBehaviour {
    public enum Axis {
        X, Y, Z
    }

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform target;
    [SerializeField] Vector3 constantForce;

    Vector3 direction;

    Vector3 startingPos;
    void Start() {
        startingPos = transform.position;
        direction = transform.position - target.position;

        // Set speed
        rb.AddForce(constantForce);
    }

    void Update() {

    }

    private void OnCollisionEnter(Collision collision) {
        rb.velocity *= 3;
    }
}
