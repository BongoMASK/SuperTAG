using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparkLightTimer : MonoBehaviour
{
    Light l;
    float timer;
    [SerializeField] int[] times;

    private void Awake() {
        l = GetComponent<Light>();
    }

    private void Update() {
        
    }

    void FlickerLights() {
        for (int i = 0; i < times.Length; i++) {
            
        }
    }
}
