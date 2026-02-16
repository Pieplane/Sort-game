using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class YandexGameReady
{
    private static bool alreadyReady = false;

    [DllImport("__Internal")]
    private static extern void GameReady();

    public static void CallGameReady()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!alreadyReady)
        {
            GameReady();
            Debug.Log("✅ GameReady() вызван — игра готова!");
            alreadyReady = true;
        }
#endif
    }
}
