using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoopGunVisuals : MonoBehaviour
{
    [SerializeField] GameObject goopPrefab;
    [SerializeField] Material goopMaterial;

    ParticleSystem part;
    List<ParticleCollisionEvent> collisionEvents;

    // Start is called before the first frame update
    void Start()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other) {
        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
        int i = 0;

        while (i < numCollisionEvents) {
            if (other.gameObject.layer == 8) {
                Vector3 pos = collisionEvents[i].intersection;
                Instantiate(goopPrefab, pos, Quaternion.identity);
            }
            i++;
        }
    }

    //TODO: find out how you can find the normal of a surface
}
