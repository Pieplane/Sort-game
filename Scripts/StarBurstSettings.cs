using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StarBurstSettings
{
    [Header("Stars")]
    public int count;          // сколько звёзд

    [Header("Spawn")]
    public float spawnRadius;  // радиус разброса от центра

    [Header("Time")]
    public bool unscaledTime;  // учитывать ли Time.timeScale

    [Header("Lifetime")]
    public float durationMin;   // например 0.45
    public float durationMax;   // например 0.70
}
