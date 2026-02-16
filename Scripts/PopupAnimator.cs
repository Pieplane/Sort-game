using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupAnimator : MonoBehaviour
{
    [SerializeField] private GameObject rootOverlay; // весь экран (серый)
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private RectTransform panel;    // окно
    [SerializeField] private CanvasGroup panelGroup;

    Tween tween;

    public void Show()
    {
        rootOverlay.SetActive(true);

        tween?.Kill();

        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;

        panel.localScale = Vector3.one * 0.85f;
        panel.anchoredPosition = panel.anchoredPosition + new Vector2(0, -40f);

        var startPos = panel.anchoredPosition;

        Sequence seq = DOTween.Sequence();
        if (overlayGroup != null) seq.Join(overlayGroup.DOFade(1f, 0.18f));
        if (panelGroup != null) seq.Join(panelGroup.DOFade(1f, 0.18f));

        seq.Join(panel.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
        seq.Join(panel.DOAnchorPos(startPos + new Vector2(0, 40f), 0.22f).SetEase(Ease.OutCubic));

        tween = seq;
    }

    public void Hide()
    {
        tween?.Kill();

        Sequence seq = DOTween.Sequence();
        if (overlayGroup != null) seq.Join(overlayGroup.DOFade(0f, 0.15f));
        if (panelGroup != null) seq.Join(panelGroup.DOFade(0f, 0.12f));

        seq.Join(panel.DOScale(0.9f, 0.15f).SetEase(Ease.InQuad));

        seq.OnComplete(() => rootOverlay.SetActive(false));
        tween = seq;
    }
}
