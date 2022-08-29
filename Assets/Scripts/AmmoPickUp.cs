using System.Collections;
using UnityEngine;
using Photon.Pun;

public class AmmoPickUp : MonoBehaviour
{
    public bool isUsable = true;

    [SerializeField] float refillTime;
    [SerializeField] Vector3 rotateSpeed;

    [SerializeField] PhotonView PV;
    [SerializeField] MeshRenderer mesh;
    [SerializeField] Collider col;

    public enum WeaponSlot {
        Primary = 0,
        Secondary
    }

    public WeaponSlot slot;

    private void Update() {
        // Cube Rotate and Up down Animation
        transform.Rotate(rotateSpeed * Time.deltaTime);
        float y = Mathf.Sin(Time.time);
        transform.localPosition = new Vector3(transform.localPosition.x, 1 + y, transform.localPosition.z);
    }

    private void OnTriggerEnter(Collider other) {
        if (!isUsable)
            return;
    }

    public void Refill(float refill) {
        PV.RPC("RPC_StartRefill", RpcTarget.AllViaServer, refill);
    }

    [PunRPC]
    void RPC_StartRefill(float refill) {
        StartCoroutine(StartRefill(refill));
    }

    IEnumerator RotateCube() {
        while (true) {
            transform.Rotate(new Vector3(1, 1, 1) * Time.deltaTime);
            yield return null;
        }
    }

    // Make Ammo PickUp Unusable for *refillTime* seconds
    IEnumerator StartRefill(float refill) {
        isUsable = false;
        mesh.enabled = false;
        col.enabled = false;
        yield return new WaitForSeconds(refill);
        isUsable = true;
        col.enabled = true;
        mesh.enabled = true;
    }
}