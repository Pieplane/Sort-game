using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FeedbackBridge : MonoBehaviour
{
    public static FeedbackBridge Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void YF_CanReview();
    [DllImport("__Internal")] private static extern void YF_RequestReview();
#endif

    public event Action<bool, string> CanReviewResult;   // (value, reason)
    public event Action<bool, string> RequestReviewResult; // (feedbackSent, error)

    public bool LastCanReviewValue { get; private set; }
    public string LastCanReviewReason { get; private set; }

    // ограничение У1 раз за сессиюФ
    bool requestedThisSession;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.name = "FeedbackBridge";
    }

    public void CanReview()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YF_CanReview();
#else
        // ¬ редакторе просто симул€ци€
        OnCanReviewResult("{\"value\":true}");
#endif
    }

    public void RequestReview()
    {
        if (requestedThisSession)
        {
            RequestReviewResult?.Invoke(false, "REVIEW_ALREADY_REQUESTED_SESSION");
            return;
        }

        requestedThisSession = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        YF_RequestReview();
#else
        OnRequestReviewResult("{\"feedbackSent\":true}");
#endif
    }

    // ---- callbacks from JS ----
    [Serializable] class CanReviewDto { public bool value; public string reason; }
    [Serializable] class RequestReviewDto { public bool feedbackSent; public string error; }

    public void OnCanReviewResult(string json)
    {
        var dto = JsonUtility.FromJson<CanReviewDto>(json);

        LastCanReviewValue = dto != null && dto.value;
        LastCanReviewReason = dto != null ? dto.reason : "UNKNOWN";

        CanReviewResult?.Invoke(LastCanReviewValue, LastCanReviewReason);
    }

    public void OnRequestReviewResult(string json)
    {
        var dto = JsonUtility.FromJson<RequestReviewDto>(json);

        bool sent = dto != null && dto.feedbackSent;
        string err = dto != null ? dto.error : null;

        RequestReviewResult?.Invoke(sent, err);
    }
}
