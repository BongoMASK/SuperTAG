using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectMovement : MonoBehaviour {
    public enum Axis {
        X, Y, Z
    }

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform target;
    [SerializeField] float constForce;

    Vector3 direction;
    Vector3 startingPos;
    void Start() {
        startingPos = transform.position;
        direction = transform.position - target.position;

        // Set speed
        rb.AddForce(direction * constForce);
    }

    private void OnCollisionEnter(Collision collision) {
        if (rb.velocity.magnitude < 5000)
            rb.velocity *= 3;
    }
}
