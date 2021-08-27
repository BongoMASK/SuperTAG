using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectScript : MonoBehaviour
{

    [SerializeField] GameObject effectBall;

    [SerializeField] Transform effectTarget;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K)) {
            ThrowBalls();
        }
    }

    void ThrowBalls() {
        GameObject ball = Instantiate(effectBall, transform.position, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        ballRb.constraints = RigidbodyConstraints.FreezePositionZ;
    }

}
