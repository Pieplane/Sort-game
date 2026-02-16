using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class LeaderboardBridge : MonoBehaviour
{
    public static LeaderboardBridge Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void LB_SetScore(string lbName, int score);
#endif

    private Action _onOk;
    private Action<string> _onFail;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.name = "LeaderboardBridge"; // ✅ для SendMessage
    }

    public void SetScore(string lbName, int score, Action onOk = null, Action<string> onFail = null)
    {
        _onOk = onOk;
        _onFail = onFail;

#if UNITY_WEBGL && !UNITY_EDITOR
        LB_SetScore(lbName, Mathf.Max(0, score));
#else
        onOk?.Invoke();
#endif
    }

    // callbacks from JS
    public void OnSetScoreOk(string _)
    {
        var cb = _onOk;
        _onOk = null; _onFail = null;
        cb?.Invoke();
    }

    public void OnSetScoreFailed(string err)
    {
        var cb = _onFail;
        _onOk = null; _onFail = null;
        cb?.Invoke(err);
    }
}
