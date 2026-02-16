using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandHintUI : MonoBehaviour
{
    [SerializeField] private RectTransform hand;   // UI Image руки
    [SerializeField] private Canvas canvas;
    //[SerializeField] private Vector2 offset = new Vector2(60, -40);
    [SerializeField] private float autoHideSeconds = 6f;

    Tween loopTween;
    float hideAt;
    bool showing;
    RectTransform currentTarget;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        HideImmediate();
    }

    public void ShowFor(RectTransform target)
    {
        if (hand == null || target == null) return;

        currentTarget = target;
        showing = true;
        gameObject.SetActive(true);

        //UpdatePosition();
        StartLoopAnim();

        hideAt = Time.unscaledTime + autoHideSeconds;
    }

    void Update()
    {
        if (!showing) return;

        // если таргет пропал/выключилс€ Ч пр€чем
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        //UpdatePosition();

        if (Time.unscaledTime >= hideAt)
            Hide();
    }

    //void UpdatePosition()
    //{
    //    var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    //    Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, currentTarget.position);
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        (RectTransform)canvas.transform, screen, cam, out var local);

    //    hand.anchoredPosition = local + offset;
    //}

    void StartLoopAnim()
    {
        loopTween?.Kill();
        hand.DOKill();

        hand.localScale = Vector3.one;
        loopTween = hand.DOScale(1.35f, 0.45f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    public void Hide()
    {
        showing = false;
        currentTarget = null;
        loopTween?.Kill();
        if (hand != null) hand.DOKill();
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        showing = false;
        currentTarget = null;
        loopTween?.Kill();
        if (hand != null) hand.DOKill();
        gameObject.SetActive(false);
    }
}
