using UnityEngine;
using Photon.Pun;

public class ProjectileGun : Gun {

    //GameObjects
    [SerializeField] GameObject bullet, camera;
    
    //speed
    [SerializeField] float speedx, speedy;
    [SerializeField] float constantForce = 1000;

    public override void Use() {
        Shoot();
    }

    void Shoot() {
        if (fireCountdown <= 0f && currentAmmo > 0) {
            Vector3 direction = camera.transform.forward;
            Physics.Raycast(shootingPoint.position, direction, out RaycastHit hit, Mathf.Infinity);
            Vector3 speed;
            bool isHit = hit.transform != null;

            if (isHit) {
                speed = GetDirection(hit.point, shootingPoint.position);
            }
            else {
                speed = direction * speedx;
                speed += Vector3.up * speedy;
            }

            PV.RPC("RPC_SpawnProjectile", RpcTarget.AllViaServer, shootingPoint.position, speed, isHit);

            //doing it to all makes multiple instances of the same object
            //create the bullet on the side from where it was shot
 
            fireCountdown = fireRate;
            reloadCountdown = reloadTime;
            currentAmmo--;
        }
    }

    Vector3 GetDirection(Vector3 destinationVector, Vector3 startVector) {  //gets direction between 2 vectors
        Vector3 direction = destinationVector - startVector;
        direction.Normalize();
        return direction;
    }

    [PunRPC]
    void RPC_SpawnProjectile(Vector3 shootingPoint, Vector3 bulletSpeed, bool isHit) {  //creates bullet for others
        GameObject b = Instantiate(bullet, shootingPoint, Quaternion.identity);
        Rigidbody rb = b.GetComponentInChildren<Rigidbody>();
        if(isHit) rb.AddForce(bulletSpeed * constantForce);
        else rb.AddForce(bulletSpeed);
    }

    [PunRPC]
    void RPC_ChangeValues(string name, int newValue) {  //changes values for console
        ChangingValues(name, newValue);
    }

    public void ChangeValues(string name, int newValue, string _weaponName) {
        if (!PhotonNetwork.IsMasterClient) return;
        if (weaponName != _weaponName) return;

        PV.RPC("RPC_ChangeValues", RpcTarget.AllBuffered, name, newValue);
    }
}
