using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnims : MonoBehaviour
{
    [SerializeField] GameObject player, orientation;

    Rigidbody playerRb;
    Animator animator;

    float pi = 3.14159f;

    private void Awake() {
        playerRb = player.GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        ChangeAnim();
    }

    void ChangeAnim() {
        Vector2 playerVel = new Vector2(playerRb.velocity.x, playerRb.velocity.z);
        Vector2 orientaionPos = new Vector2(orientation.transform.forward.x, orientation.transform.forward.z);

        float angle = FindAngle(playerVel, orientaionPos);

        /*
        Logic
        use orientation.transform.forward and player velocity
        do dot product and find out angle between them 
        based on the angle do animation
        */

        bool isForward = animator.GetBool("forward");

        if (!isForward && angle > -45 && angle < 45) {
            animator.SetBool("forward", true);
            Debug.Log("moving straight");
            //moving straight
        }

        if (angle > 45 && angle < 135) {
            if (playerVel.x * orientaionPos.y > 0) {    //checks if player moves left or right
                animator.SetBool("right", true);
                Debug.Log("moving right");
                //moving right
            }

            else {
                animator.SetBool("left", true);
                Debug.Log("moving left");
                //moving left
            }
        }
        if (angle > 135 && angle < 225) {
            animator.SetBool("backward", true);
            Debug.Log("moving back");
            //moving back
        }
    }

    float FindAngle(Vector3 vec1, Vector3 vec2) {
        float dot = (vec1.x * vec2.x) + (vec1.y * vec2.y);  //dot product
        float den = GetMagnitude(vec1) * GetMagnitude(vec2);    //magnitude product
        float result = Mathf.Acos(dot / den);
        result *= 180 / pi;     //convert radian to degrees
        return result;
    }

    float GetMagnitude(Vector3 vec1) {
        return Mathf.Sqrt(vec1.x * vec1.x + vec1.y * vec1.y);
    }
}
