using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulse : MonoBehaviour
{
    public float pulseSpeed = 2f;     // 
    public float pulseScale = 0.05f;  //  (0.05 = ±5%)

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = initialScale * (1f + scaleOffset);
    }
}
