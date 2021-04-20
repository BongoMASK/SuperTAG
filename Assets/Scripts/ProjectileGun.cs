using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class ProjectileGun : Gun {

    [SerializeField] string weaponName;

    //GameObjects
    [SerializeField] GameObject bullet, shootingPoint, camera;
    [SerializeField] Slider ammoSlider;
    
    //speed
    [SerializeField] float speedx, speedy;

    //Ammo
    int currentAmmo;
    [SerializeField] int maxAmmo = 3;

    //Reload / Firing
    [SerializeField] bool reloadWhenInactive = false;
    [SerializeField] float reloadTime = 2, fireRate = 1;
    float fireCountdown = 0, reloadCountdown;

    //Text
    [SerializeField] TMP_Text ammoText;
    [SerializeField] TMP_Text weaponText;

    //Color
    [SerializeField] Color32 weaponColor;

    PhotonView PV;

    public override void Use() {
        Shoot();
    }

    private void Awake() {
        currentAmmo = maxAmmo;
        reloadCountdown = reloadTime;
        PV = GetComponent<PhotonView>();
    }

    private void Update() {
        if (itemGameObject.activeSelf) {
            if(!reloadWhenInactive) {
                Reload();
            }
            ammoSlider.maxValue = reloadTime;
            ammoSlider.value = reloadTime - reloadCountdown;
        }
        fireCountdown -= Time.deltaTime;

        if (itemGameObject.activeSelf) {
            ammoText.color = weaponColor;
            weaponText.color = weaponColor;
            ammoText.text = currentAmmo.ToString();
            weaponText.text = gameObject.name;
        }

        if (reloadWhenInactive) Reload();
    }

    void Shoot() {
        if (fireCountdown <= 0f && currentAmmo > 0) {
            Vector3 direction = camera.transform.forward;
            PV.RPC("RPC_SpawnProjectile", RpcTarget.AllViaServer, shootingPoint.transform.position, direction);   

            //doing it to all makes multiple                                                                                        
            //instances of the same object
            //create the bullet on the side from where it was shot
 
            fireCountdown = fireRate;
            reloadCountdown = reloadTime;
            currentAmmo--;
        }
    }

    void Reload() {
        if (currentAmmo < maxAmmo) {
            if (reloadCountdown <= 0f) {
                currentAmmo++;
                reloadCountdown = reloadTime;
            }
            reloadCountdown -= Time.deltaTime;
        }
    }

    [PunRPC]
    void RPC_SpawnProjectile(Vector3 shootingPoint, Vector3 cameraPoint) {      //creates bullet for others
        GameObject b = Instantiate(bullet, shootingPoint, Quaternion.identity);
        Rigidbody rb = b.GetComponentInChildren<Rigidbody>();
        rb.AddForce(cameraPoint * speedx);
        rb.AddForce(Vector2.up * speedy);
    }

    [PunRPC]
    void RPC_ChangeValues(string name, int newValue, string _weaponName) {
        if (name == nameof(maxAmmo)) maxAmmo = newValue;
        if (name == nameof(reloadTime)) reloadTime = newValue;
        if (name == nameof(fireRate)) fireRate = newValue;
    }

    public void ChangeValues(string name, int newValue, string _weaponName) {
        if (!PhotonNetwork.IsMasterClient) return; 
        if (weaponName != _weaponName) return;

        PV.RPC("RPC_ChangeValues", RpcTarget.AllBuffered, name, newValue, _weaponName);
    }

    public void ChangeValues(string name, bool newValue, string _weaponName) {
        if (weaponName != _weaponName) return;

        if (name == nameof(reloadWhenInactive)) reloadWhenInactive = newValue;
    }


}
