using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunPulseRotation : MonoBehaviour
{

    public float rotationSpeed = 30f; // ãðàäóñîâ â ñåêóíäó


    public float pulseSpeed = 2f;     // ñêîðîñòü ïóëüñàöèè
    public float pulseScale = 0.05f;  // àìïëèòóäà ïóëüñàöèè (0.05 = ±5%)

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {

        transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);


        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = initialScale * (1f + scaleOffset);
    }
}
