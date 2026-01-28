using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CollectionSave
{
    public int currentIndex;
    public float[] progress;     // размер = items.Count
    public int pendingTriples;
}
