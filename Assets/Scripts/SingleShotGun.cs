using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam;

    public override void Use() {
        Debug.Log("using " + itemInfo.itemName);
        Shoot();
    }

    void Shoot() {
        Ray ray = cam.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        ray.origin = cam.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Debug.Log("we hit " + hit.collider.gameObject.name + "  " + hit.collider.gameObject.layer);
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
        }
        else Debug.Log("didnt hit.");
    }
}
