using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LosePanelUI : MonoBehaviour
{
    [Header("Root (overlay)")]
    [SerializeField] private GameObject root;          // серый фон на весь экран
    [SerializeField] private CanvasGroup overlayGroup; // CanvasGroup на root

    [Header("Popup panel")]
    [SerializeField] private RectTransform popupPanel; // внутренняя панель проигрыша
    [SerializeField] private CanvasGroup panelGroup;   // CanvasGroup на popupPanel

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button continueButton;

    private Sequence showSeq;
    private Sequence hideSeq;
    private bool isShowing;

    public System.Action OnRestart;
    public System.Action OnContinue;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => OnRestart?.Invoke());

        if (continueButton != null)
            continueButton.onClick.AddListener(() => OnContinue?.Invoke());

        HideImmediate();
    }

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;

        root.SetActive(true);

        showSeq?.Kill();
        hideSeq?.Kill();

        overlayGroup.alpha = 0f;
        panelGroup.alpha = 0f;
        popupPanel.localScale = Vector3.one * 0.85f;

        showSeq = DOTween.Sequence()
            .Append(overlayGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad))
            .Join(panelGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad))
            .Join(popupPanel.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;

        showSeq?.Kill();
        hideSeq?.Kill();

        hideSeq = DOTween.Sequence()
            .Append(panelGroup.DOFade(0f, 0.12f).SetEase(Ease.InQuad))
            .Join(popupPanel.DOScale(0.9f, 0.12f).SetEase(Ease.InQuad))
            .Join(overlayGroup.DOFade(0f, 0.12f).SetEase(Ease.InQuad))
            .OnComplete(() => root.SetActive(false));
    }

    public void HideImmediate()
    {
        showSeq?.Kill();
        hideSeq?.Kill();

        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;
        if (popupPanel != null) popupPanel.localScale = Vector3.one;

        isShowing = false;
        if (root != null) root.SetActive(false);
    }
}
