using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    [SerializeField] Transform camera;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        Debug.Log(mousePos);
        mousePos.z = camera.transform.position.z;
        transform.LookAt(mousePos);
    }
}
