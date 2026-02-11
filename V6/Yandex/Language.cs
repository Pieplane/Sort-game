using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Language : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string GetLang();

    public string CurrentLanguage;

    public static Language Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_WEBGL && !UNITY_EDITOR
        CurrentLanguage = GetLang();
        Debug.Log($"Текущий домен для языка +{CurrentLanguage}");
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
