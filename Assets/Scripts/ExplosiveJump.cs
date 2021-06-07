using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveJump : MonoBehaviour
{
    [SerializeField] float jumpForce, radiusOfEffect;
    [SerializeField] Vector3 offset = new Vector3(0, 1, 0);

    [SerializeField] GameObject impactField;

    private void Start() {
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.layer == 8) 
            Explode();
    }

    private void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.layer == 8)
            Explode();
    }

    void Explosion(GameObject other) {
        Vector3 direction = GetDirection(other.transform.position, transform.position);
        Rigidbody rb = other.GetComponent<Rigidbody>();
        Vector3 force = 10 * jumpForce * direction;
        rb.AddForce(force);
    }

    void Explode() {
        GameObject effect = Instantiate(impactField, transform.position + offset, Quaternion.identity);
        Destroy(effect, 2f);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radiusOfEffect);  
        foreach (Collider collider in hitColliders) {
            if(collider.gameObject.GetComponent<Rigidbody>()) {
                Explosion(collider.gameObject);
            }
        }
        Destroy(gameObject.transform.parent.gameObject);
    }

    float GetDist(Vector3 p1, Vector3 p2) {
        float distance = Mathf.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y) + (p1.z - p2.z) * (p1.z - p2.z));
        return distance;
    }

    Vector3 GetDirection(Vector3 destinationVector, Vector3 startVector) {
        Vector3 direction = destinationVector - startVector;
        direction.Normalize();
        return direction;
    }
}