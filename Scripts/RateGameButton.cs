using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RateGameButton : MonoBehaviour
{
    [SerializeField] private Button button;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        Refresh();
    }

    void Refresh()
    {
        // по умолчанию включена, но можно отключать после canReview=false
        if (button != null) button.interactable = true;
    }

    void OnClick()
    {
        AudioManager.Instance?.Play("Click");

        if (FeedbackBridge.Instance == null)
        {
            Debug.LogWarning("FeedbackBridge not found");
            return;
        }

        // 1) об€зательно canReview
        FeedbackBridge.Instance.CanReviewResult -= OnCanReview;
        FeedbackBridge.Instance.CanReviewResult += OnCanReview;
        FeedbackBridge.Instance.CanReview();
    }

    void OnCanReview(bool value, string reason)
    {
        FeedbackBridge.Instance.CanReviewResult -= OnCanReview;

        if (!value)
        {
            // причина: NO_AUTH / GAME_RATED / REVIEW_ALREADY_REQUESTED / REVIEW_WAS_REQUESTED / UNKNOWN
            Debug.Log("[Feedback] Can't request review: " + reason);

            // хочешь Ч отключай кнопку навсегда/на сессию
            if (button != null) button.interactable = false;
            return;
        }

        // 2) requestReview
        FeedbackBridge.Instance.RequestReviewResult -= OnRequestReview;
        FeedbackBridge.Instance.RequestReviewResult += OnRequestReview;

        FeedbackBridge.Instance.RequestReview();
    }

    void OnRequestReview(bool feedbackSent, string error)
    {
        FeedbackBridge.Instance.RequestReviewResult -= OnRequestReview;

        Debug.Log("[Feedback] requestReview result: feedbackSent=" + feedbackSent + " error=" + error);

        // после попытки можно выключить кнопку (часто логично)
        if (button != null) button.interactable = false;
    }
}
