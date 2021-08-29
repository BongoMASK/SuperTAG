using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectMovement : MonoBehaviour {

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform target;
    [SerializeField] float constForce;

    Vector3 direction;

    void Start() {
        direction = transform.position - target.position;

        // Set speed
        rb.AddForce(direction * constForce);
    }

    private void OnCollisionEnter(Collision collision) {
        if (rb.velocity.magnitude < 50)
            rb.velocity *= 3;
    }
}
