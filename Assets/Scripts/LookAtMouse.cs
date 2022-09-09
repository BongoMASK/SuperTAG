using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    [SerializeField] Transform cam;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        Debug.Log(mousePos);
        mousePos.z = cam.transform.position.z;
        transform.LookAt(mousePos);
    }
}
