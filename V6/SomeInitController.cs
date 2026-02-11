using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeInitController : MonoBehaviour
{
    IEnumerator Start()
    {
        // ждём 1 кадр, чтобы Unity дорисовал UI
        yield return null;

        YandexGameReady.CallGameReady();
    }
}
