using Photon.Pun;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public abstract class Gun : Item {
 
    [SerializeField] protected string weaponName;
    [SerializeField] protected Transform shootingPoint;

    //Ammo
    protected int currentAmmo;
    [SerializeField] protected int maxAmmo;

    //Reload / Firing
    [SerializeField] protected bool reloadWhenInactive = false;
    [SerializeField] protected float reloadTime = 2, fireRate = 1;
    protected float fireCountdown = 0f, reloadCountdown;

    [SerializeField] Slider ammoSlider;

    protected PhotonView PV;    

    //Text
    [SerializeField] TMP_Text ammoText;
    [SerializeField] TMP_Text weaponText;

    //Color
    [SerializeField] Color32 weaponColor;

    public abstract override void Use();

    private void Awake() {
        currentAmmo = maxAmmo;
        reloadCountdown = reloadTime;
        PV = GetComponent<PhotonView>();
    }

    private void Update() {
        if (reloadWhenInactive) Reload();

        if (itemGameObject.activeSelf) {
            if (!reloadWhenInactive) Reload();

            ammoSlider.maxValue = reloadTime;
            ammoSlider.value = reloadCountdown;

            ammoText.color = weaponColor;
            weaponText.color = weaponColor;
            ammoText.text = currentAmmo.ToString();
            weaponText.text = gameObject.name;
        }
        fireCountdown -= Time.deltaTime;
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

    protected void ChangingValues(string name, int newValue) {
        if (name == nameof(maxAmmo)) maxAmmo = newValue;
        if (name == nameof(reloadTime)) reloadTime = newValue;
        if (name == nameof(fireRate)) fireRate = newValue;
        if (name == nameof(reloadWhenInactive)) reloadWhenInactive = IntToBool(newValue);

        Message.message("Changed " + weaponName + "'s " + name + " to: " + newValue);
    }

    bool IntToBool(int value) {
        if (value == 0) return false;
        else return true;
    }
}
 