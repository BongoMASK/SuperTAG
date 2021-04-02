using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] PhysicMaterial sticky;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.layer == 8) {
            rb.isKinematic = true;
            if(collision.gameObject.GetComponent<BoxCollider>()) 
                collision.gameObject.GetComponent<BoxCollider>().material = sticky;
            else 
                collision.gameObject.GetComponent<MeshCollider>().material = sticky;

        }
    }
}
