using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncreaseSize : MonoBehaviour {
    [SerializeField] float constant = 3.6f;
    [SerializeField] float expandRate = 3, destroyAfter = 0.6f;

    [SerializeField] GameObject otherGameObject;

    [SerializeField] Vector3 largestPos = new Vector3(1, 1, 1);
    Vector3 currentScale;
    float currentPos, timer = 0.1f;

    void Start() {
        currentScale = transform.localScale;
        currentPos = transform.position.x;
        Destroy(gameObject, destroyAfter);
    }

    void Update() {
        timer += Time.deltaTime;
        float lerpConst = timer * expandRate;
        transform.localScale = Vector3.Lerp(currentScale, largestPos, lerpConst);
        transform.position = new Vector3(currentPos + transform.localScale.x * constant, transform.position.y, transform.position.z);

        if (otherGameObject != null) {
            otherGameObject.transform.position = new Vector3(currentPos - transform.localScale.x * constant * 0.5f,
                otherGameObject.transform.position.y, otherGameObject.transform.position.z);
        }

    }
}