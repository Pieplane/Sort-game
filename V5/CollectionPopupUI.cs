using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;          // серый фон на весь экран
    [SerializeField] private CanvasGroup overlayGroup; // CanvasGroup на root

    [Header("Popup Panel")]
    [SerializeField] private RectTransform popupPanel; // внутренняя панель
    [SerializeField] private CanvasGroup panelGroup;   // CanvasGroup на popupPanel

    [Header("Content")]
    [SerializeField] private Image silhouette;
    [SerializeField] private Image reveal; // Filled
    [SerializeField] private Button collectButton;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject sun;

    public System.Action OnCollectClicked;

    private Sequence showSeq;

    private void Awake()
    {
        collectButton.onClick.AddListener(() => OnCollectClicked?.Invoke());
        HideImmediate();
    }

    public void ShowPreview(Sprite itemSprite, float current01, float target01, float step01)
    {
        silhouette.sprite = itemSprite;
        silhouette.color = Color.black;

        reveal.sprite = itemSprite;
        reveal.color = Color.white;

        reveal.type = Image.Type.Filled;
        reveal.fillMethod = Image.FillMethod.Vertical;
        reveal.fillOrigin = (int)Image.OriginVertical.Bottom;
        reveal.fillAmount = Mathf.Clamp01(target01);

        if (progressText != null)
        {
            int tar = Mathf.RoundToInt(target01 * 100f);
            progressText.text = $"{tar}%";
        }

        ShowAnimated();
    }

    private void ShowAnimated()
    {
        root.SetActive(true);
        sun?.SetActive(false);

        // стоп прошлых анимаций
        showSeq?.Kill();

        // стартовые значения
        overlayGroup.alpha = 0f;
        panelGroup.alpha = 0f;
        popupPanel.localScale = Vector3.one * 0.85f;

        showSeq = DOTween.Sequence();

        // 1) фон: плавно проявить
        showSeq.Append(overlayGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));

        // 2) панель: scale + fade
        showSeq.Join(panelGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));
        showSeq.Join(popupPanel.DOScale(1f, 0.28f).SetEase(Ease.OutBack));

        showSeq.Play();
    }

    public void Hide()
    {
        showSeq?.Kill();
        root.SetActive(false);
        BoardManager.Instance?.CloseModal();
    }

    public void HideImmediate()
    {
        showSeq?.Kill();

        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;
        if (popupPanel != null) popupPanel.localScale = Vector3.one;

        root.SetActive(false);
    }

    public IEnumerator PlayCompleteEffectDOTween()
    {
        collectButton.interactable = false;
        if (sun != null) sun.SetActive(true);

        reveal.fillAmount = 1f;
        if (progressText != null)
            progressText.text = "Предмет собран!";

        Sequence seq = DOTween.Sequence();

        seq.Append(reveal.transform.DOScale(1.15f, 0.18f).SetEase(Ease.OutBack));
        seq.Join(reveal.DOFade(0.6f, 0.1f).SetLoops(2, LoopType.Yoyo));
        seq.Append(reveal.transform.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad));
        seq.AppendInterval(0.6f);

        yield return seq.WaitForCompletion();

        collectButton.interactable = true;
        if (sun != null) sun.SetActive(false);
    }
}
