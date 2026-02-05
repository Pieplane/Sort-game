using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoostShopPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;          // серый фон на весь экран
    [SerializeField] private CanvasGroup overlayGroup; // CanvasGroup на root

    [Header("Popup Panel")]
    [SerializeField] private RectTransform popupPanel; // внутренняя панель
    [SerializeField] private CanvasGroup panelGroup;   // CanvasGroup на popupPanel

    [Header("UI")]
    [SerializeField] private Button closeButton;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text ownedText;

    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyOneButton;

    [SerializeField] private TMP_Text buyTwoPriceText;
    [SerializeField] private Button buyTwoButton;

    [Header("Prices")]
    [SerializeField] private int clearSomePrice = 100;    // цена 1 буста
    [SerializeField] private int clearSomePriceX2 = 180;  // цена за 2 (пока без рекламы)
    [SerializeField] private int shufflePrice = 120;
    [SerializeField] private int shufflePriceX2 = 120;
    [SerializeField] private int hintThreeSamePrice = 120;
    [SerializeField] private int hintThreeSamePriceX2 = 120;

    public bool debugLog = false;

    private BoostType currentType;
    private Action onChanged;

    private Sequence seq;
    private Vector2 panelStartPos;

    Tween tween;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (buyOneButton != null) buyOneButton.onClick.AddListener(BuyOne);
        if (buyTwoButton != null) buyTwoButton.onClick.AddListener(BuyTwo);

        if (popupPanel != null)
            panelStartPos = popupPanel.anchoredPosition;

    }

    // Открыть панель для конкретного буста
    public void OpenFor(BoostType type, Action onChanged)
    {
        currentType = type;
        this.onChanged = onChanged;

        Refresh();
        Show();
    }

    // ================== SHOW / HIDE ==================

    public void Show()
    {
        root.SetActive(true);

        tween?.Kill();

        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;



        popupPanel.localScale = Vector3.one * 0.85f;
        popupPanel.anchoredPosition = popupPanel.anchoredPosition + new Vector2(0, -40f);

        var startPos = popupPanel.anchoredPosition;

        Sequence seq = DOTween.Sequence();
        if (overlayGroup != null) seq.Join(overlayGroup.DOFade(1f, 0.18f));
        if (panelGroup != null) seq.Join(panelGroup.DOFade(1f, 0.18f));

        seq.Join(popupPanel.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
        seq.Join(popupPanel.DOAnchorPos(startPos + new Vector2(0, 40f), 0.22f).SetEase(Ease.OutCubic));

        tween = seq;
    }

    public void Close()
    {
        Hide();
    }

    public void Hide()
    {
        tween?.Kill();

        Sequence seq = DOTween.Sequence();
        if (overlayGroup != null) seq.Join(overlayGroup.DOFade(0f, 0.15f));
        if (panelGroup != null) seq.Join(panelGroup.DOFade(0f, 0.12f));

        seq.Join(popupPanel.DOScale(0.9f, 0.15f).SetEase(Ease.InQuad));

        seq.OnComplete(() => root.SetActive(false));
        tween = seq;
    }


    private void Refresh()
    {
        int owned = BoostInventory.Get(currentType);

        if (titleText != null)
            titleText.text = currentType == BoostType.ClearSomeOfType ? "Удалить предметы" : currentType.ToString();

        if (ownedText != null)
            ownedText.text = $"{owned}";

        int p1 = GetPrice1(currentType);
        int p2 = GetPrice2(currentType);

        if (priceText != null) priceText.text = p1.ToString();
        //if (buyTwoPriceText != null) buyTwoPriceText.text = p2.ToString();

        int coins = (PlayerProgress.Instance != null) ? PlayerProgress.Instance.Coins : 0;

        if (buyOneButton != null) buyOneButton.interactable = coins >= p1;
        if (buyTwoButton != null) buyTwoButton.interactable = coins >= p2;

        if (debugLog)
            Debug.Log($"BoostShop Refresh: type={currentType}, owned={owned}, coins={coins}, p1={p1}, p2={p2}");
    }

    private int GetPrice1(BoostType t) => t switch
    {
        BoostType.ClearSomeOfType => clearSomePrice,
        BoostType.ShuffleButton => shufflePrice,
        BoostType.HintThreeSame => hintThreeSamePrice,
        _ => 999
    };

    private int GetPrice2(BoostType t) => t switch
    {
        BoostType.ClearSomeOfType => clearSomePriceX2,
        BoostType.ShuffleButton => shufflePriceX2,
        BoostType.HintThreeSame => hintThreeSamePriceX2,
        _ => 999
    };

    private void BuyOne()
    {
        int price = GetPrice1(currentType);
        if (!TrySpendCoins(price)) return;

        BoostInventory.Add(currentType, 1);
        Refresh();
        onChanged?.Invoke();
    }

    private void BuyTwo()
    {
        int price = GetPrice2(currentType);
        if (!TrySpendCoins(price)) return;

        BoostInventory.Add(currentType, 2);
        Refresh();
        onChanged?.Invoke();
    }

    private bool TrySpendCoins(int amount)
    {
        if (PlayerProgress.Instance == null) return false;
        return PlayerProgress.Instance.TrySpendCoins(amount);
    }

    void OnDisable()
    {
        seq?.Kill();
        DOTween.Kill(gameObject);
        transform.DOKill(true);
    }
}
