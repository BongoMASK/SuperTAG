using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnims : MonoBehaviour
{
    [SerializeField] GameObject player;
    Rigidbody playerRb;

    private void Awake() {
        playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        
    }

    void ChangeAnim() {
        if(playerRb.velocity.x > 1) {
            //play 
        }
    }
}
