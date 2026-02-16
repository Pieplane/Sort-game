using UnityEngine;
using System.Runtime.InteropServices;

public static class StickyAds
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void IAP_ShowSticky();
    [DllImport("__Internal")] private static extern void IAP_HideSticky();
#endif

    public static void Show()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_ShowSticky();
#endif
    }

    public static void Hide()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_HideSticky();
#endif
    }
}
