using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    Rigidbody rb;

    [SerializeField] Collider area; 
    [SerializeField] Collider sphereCollider;  
    [SerializeField] ParticleSystem particle;
    [SerializeField] ParticleSystem particle2;

    [SerializeField] float destroyAfter = 10f;
    [SerializeField] Vector3 areaOfEffect = new Vector3(8, 7, 8);
    [SerializeField] Vector3 otherPosition = new Vector3(0, -100, 0);

    float timer;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        area.enabled = false;
    }

    private void Start() {
        timer = destroyAfter;
        Destroy(gameObject.transform.parent.gameObject, destroyAfter);
    }

    private void Update() {
        Timer();
    }

    private void OnCollisionEnter(Collision collision) {
        Vector3 normal = collision.contacts[0].normal;
        if (normal.y <= 0.2) {
            rb.velocity *= 3;
            return;
        }
        ProcessCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other) {
        ProcessCollision(other.gameObject);
    }

    void ProcessCollision(GameObject collider) {
        if (collider.layer == 8) {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
            particle.startLifetime = timer - 1f;
            particle.Play();
            particle2.Play();
            transform.localEulerAngles = new Vector3(0, 0, 0);
            sphereCollider.enabled = false;
            area.enabled = true;

            Debug.Log("gooped");
        }
    }

    void Timer() {
        timer -= Time.deltaTime;
        if(timer <= 0.5) { 
            area.gameObject.transform.localScale = new Vector3(0, 0, 0);
            area.gameObject.transform.position = otherPosition;
        }
    }
}
