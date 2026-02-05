using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomUpgradePanel : MonoBehaviour
{
    [Header("Root/Anim (как твой PopupAnimator)")]
    [SerializeField] private GameObject rootOverlay;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;

    [Header("Cards (3 variants)")]
    [SerializeField] private VariantCardUI[] cards = new VariantCardUI[3];

    //[Header("Optional FX")]
    //[SerializeField] private StarFxPool starFxPool;
    //[SerializeField] private Canvas fxCanvas;
    //[SerializeField] private StarBurstPreset buyFxPreset = StarBurstPreset.Default;

    Tween tween;

    RoomItemDefinition currentDef;

    private readonly List<int> sessionBadges = new(); // какие idx получили "!" в этой сессии

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        HideImmediate();
    }

    public void Open(RoomItemDefinition def)
    {
        currentDef = def;
        Refresh();
        Show();
    }

    void Refresh()
    {
        if (currentDef == null || currentDef.variants == null) return;

        if (titleText != null)
            titleText.text = currentDef.displayName;

        sessionBadges.Clear();

        int equipped = RoomProgress.GetEquipped(currentDef.id);
        int playerLevel = PlayerProgress.Instance != null ? PlayerProgress.Instance.Level : 1;

        int max = Mathf.Min(cards.Length, currentDef.variants.Length); // ✅ защита

        for (int i = 0; i < max; i++)
        {
            int idx = i; // ✅ КЛЮЧЕВО: фиксируем индекс 0..2

            var card = cards[idx];
            if (card == null) continue;

            var v = currentDef.variants[idx];

            if (card.icon != null)
                card.icon.sprite = v.previewSprite != null ? v.previewSprite : v.sceneSprite;

            if (card.titleText != null)
                card.titleText.text = string.IsNullOrEmpty(v.title) ? $"Вариант {idx}" : v.title;

            bool byLevel = (idx == 0) || (playerLevel >= v.unlockLevel);
            bool opened = RoomProgress.IsUnlocked(currentDef.id, idx);
            bool unlocked = byLevel || opened;
            bool owned = RoomProgress.IsOwned(currentDef.id, idx);
            bool isEquipped = (equipped == idx);

            // ===== NEW BADGE "!" =====
            // показываем "!" только для "за ранг":
            //  - idx != 0
            //  - playerLevel >= unlockLevel (byLevel)
            //  - НЕ opened (то есть не открывали за рекламу/навсегда)
            //  - не куплено
            //  - ещё не видели
            bool unlockedByRank = (idx != 0) && byLevel && !opened;
            bool showNew = unlockedByRank && !owned && !RoomProgress.IsSeen(currentDef.id, idx);

            if (card.newBadgeRoot != null)
                card.newBadgeRoot.SetActive(showNew);

            if (showNew)
                sessionBadges.Add(idx);

            if (card.lockRoot != null) card.lockRoot.SetActive(!unlocked);
            if (card.lockText != null) card.lockText.text = (!unlocked) ? $"Ранг {v.unlockLevel}" : "";

            // ✅ визуально показываем "закрыто" через затемнение/силуэт (опционально)
            //if (card.icon != null)
            //{
            //    var c = card.icon.color;
            //    c.a = unlocked ? 1f : 0.15f;
            //    card.icon.color = c;
            //}

            if (card.ownedRoot != null) card.ownedRoot.SetActive(owned && !isEquipped);
            if (card.equippedRoot != null) card.equippedRoot.SetActive(isEquipped);

            if (card.priceText != null)
                card.priceText.text = (!unlocked || owned || isEquipped || idx == 0 || v.priceCoins <= 0) ? "" : v.priceCoins.ToString();

            // ✅ вместо actionButton
            ClearActionListeners(card);
            HideAllActionButtons(card);

            // 1) дефолт
            if (idx == 0)
            {
                if (isEquipped)
                {
                    if (card.btnInstalledRoot) card.btnInstalledRoot.SetActive(true);
                    //if (card.btnInstalled) card.btnInstalled.interactable = false;
                }
                else
                {
                    if (card.btnEquipRoot) card.btnEquipRoot.SetActive(true);
                    if (card.btnEquip) card.btnEquip.onClick.AddListener(() => EquipVariant(0));
                }
                continue;
            }

            // 2) не открыт => открыть (за рекламу / или просто открыть)
            if (!unlocked)
            {
                if (card.btnUnlockAdRoot) card.btnUnlockAdRoot.SetActive(true);
                if (card.btnUnlockAd) card.btnUnlockAd.onClick.AddListener(() => UnlockAdAndEquip(idx));
                continue;
            }

            // 3) открыт, но не куплен => купить
            if (!owned)
            {
                if (card.btnBuyRoot) card.btnBuyRoot.SetActive(true);

                bool canBuy = PlayerProgress.Instance != null && PlayerProgress.Instance.Coins >= v.priceCoins;
                if (card.btnBuy) card.btnBuy.interactable = canBuy;

                if (canBuy && card.btnBuy)
                    card.btnBuy.onClick.AddListener(() => BuyVariant(idx));

                continue;
            }

            // 4) куплен => установить / установлено
            if (isEquipped)
            {
                if (card.btnInstalledRoot) card.btnInstalledRoot.SetActive(true);
                //if (card.btnInstalled) card.btnInstalled.interactable = false;
            }
            else
            {
                if (card.btnEquipRoot) card.btnEquipRoot.SetActive(true);
                if (card.btnEquip) card.btnEquip.onClick.AddListener(() => EquipVariant(idx));
            }
        }
    }

    //void SetAction(VariantCardUI card, string label, bool interactable, System.Action onClick)
    //{
    //    //if (card.actionText != null) card.actionText.text = label;
    //    card.actionButton.interactable = interactable;
    //    if (onClick != null) card.actionButton.onClick.AddListener(() => onClick());
    //}
    void HideAllActionButtons(VariantCardUI card)
    {
        if (card.btnInstalledRoot) card.btnInstalledRoot.SetActive(false);
        if (card.btnEquipRoot) card.btnEquipRoot.SetActive(false);
        if (card.btnBuyRoot) card.btnBuyRoot.SetActive(false);
        if (card.btnUnlockAdRoot) card.btnUnlockAdRoot.SetActive(false);
    }

    void ClearActionListeners(VariantCardUI card)
    {
        if (card.btnInstalled) card.btnInstalled.onClick.RemoveAllListeners();
        if (card.btnEquip) card.btnEquip.onClick.RemoveAllListeners();
        if (card.btnBuy) card.btnBuy.onClick.RemoveAllListeners();
        if (card.btnUnlockAd) card.btnUnlockAd.onClick.RemoveAllListeners();
    }
    void UnlockVariant(int variantIndex)
    {
        if (currentDef == null) return;

        // ✅ открываем навсегда, и после этого ранг не важен
        RoomProgress.SetUnlocked(currentDef.id, variantIndex, true);

        // можно маленький pop/звезды на карточке позже
        Refresh();
    }

    void BuyVariant(int variantIndex)
    {
        if (currentDef == null || currentDef.variants == null)
        {
            Debug.LogError("BuyVariant: currentDef or variants is null");
            return;
        }

        if (variantIndex < 0 || variantIndex >= currentDef.variants.Length)
        {
            Debug.LogError($"BuyVariant: BAD index={variantIndex}, variants.Length={currentDef.variants.Length}, def={currentDef.name}");
            return;
        }

        var v = currentDef.variants[variantIndex];
        if (PlayerProgress.Instance == null) return;

        // защита ранга (если не открыт навсегда и уровень мал)
        bool opened = RoomProgress.IsUnlocked(currentDef.id, variantIndex);
        if (!opened && PlayerProgress.Instance.Level < v.unlockLevel)
            return;

        // денег нет -> ничего не меняем, панель не закрываем
        if (!PlayerProgress.Instance.TrySpendCoins(v.priceCoins))
            return;

        // ✅ покупка успешна
        RoomProgress.SetOwned(currentDef.id, variantIndex, true);

        // ✅ сразу ставим как установленный
        RoomProgress.SetEquipped(currentDef.id, variantIndex, RoomProgress.UpgradeSource.Buy);
        //RoomVisualManager.Instance?.Apply(currentDef, variantIndex);

        // опционально: обновить UI на 1 кадр перед закрытием (можно убрать)
        Refresh();

        // ✅ и закрываем панель
        Close(); // или HideImmediate();
    }

    void EquipVariant(int variantIndex)
    {
        if (currentDef == null) return;

        // дефолт всегда можно, остальные только если owned
        if (variantIndex != 0 && !RoomProgress.IsOwned(currentDef.id, variantIndex))
            return;

        RoomProgress.SetEquipped(currentDef.id, variantIndex, RoomProgress.UpgradeSource.Equip);
        //RoomVisualManager.Instance?.Apply(currentDef, variantIndex);
        // тут ты можешь дернуть RoomVisualManager чтобы в комнате сменить объект/спрайт
        // RoomVisualManager.Instance.Apply(currentDef.id, variantIndex);

        Refresh();
    }

    //void ShakeLocked(VariantCardUI card, int variantIndex)
    //{
    //    // потрясти карточку (или замок)
    //    var rt = card != null ? card.GetComponent<RectTransform>() : null;
    //    if (rt == null) return;

    //    rt.DOKill();
    //    rt.DOShakeAnchorPos(0.35f, new Vector2(18f, 0f), 14, 0f, false, true)
    //      .SetEase(Ease.OutQuad)
    //      .SetUpdate(true);
    //}

    //void PlayFxOnCard(int variantIndex)
    //{
    //    if (starFxPool == null || fxCanvas == null) return;
    //    if (variantIndex < 0 || variantIndex >= cards.Length) return;
    //    var t = cards[variantIndex] != null ? cards[variantIndex].transform : null;
    //    if (t == null) return;

    //    // ✅ правильно: worldPos + canvas => в localPos fxRoot
    //    starFxPool.PlayBurstFromWorld(t.position, fxCanvas, buyFxPreset);
    //}

    // ====== SHOW/HIDE ======

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
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void Close()
    {
        MarkBadgesSeen(); // ✅ чтобы "!" больше не показывался после закрытия

        tween?.Kill();

        Sequence seq = DOTween.Sequence();
        if (overlayGroup != null) seq.Join(overlayGroup.DOFade(0f, 0.15f));
        if (panelGroup != null) seq.Join(panelGroup.DOFade(0f, 0.12f));

        seq.Join(panel.DOScale(0.9f, 0.15f).SetEase(Ease.InQuad));
        seq.OnComplete(() => rootOverlay.SetActive(false));

        tween = seq;
    }

    public void HideImmediate()
    {
        MarkBadgesSeen(); // ✅ чтобы "!" больше не показывался после закрытия

        tween?.Kill();
        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;
        if (panel != null) panel.localScale = Vector3.one;
        if (rootOverlay != null) rootOverlay.SetActive(false);
    }
    void UnlockAdAndEquip(int variantIndex)
    {
        if (currentDef == null || currentDef.variants == null) return;
        if (variantIndex <= 0 || variantIndex >= currentDef.variants.Length) return; // 0 — дефолт не трогаем

        // ✅ 1) открыть навсегда
        RoomProgress.SetUnlocked(currentDef.id, variantIndex, true);

        // ✅ 2) "купить" бесплатно
        RoomProgress.SetOwned(currentDef.id, variantIndex, true);

        // ✅ 3) сразу установить
        RoomProgress.SetEquipped(currentDef.id, variantIndex, RoomProgress.UpgradeSource.Buy);
        //RoomVisualManager.Instance?.Apply(currentDef, variantIndex);

        // UI можно обновить, но если закрываешь сразу — это опционально
        Refresh();

        // ✅ 4) закрываем панель (если хочешь)
        Close(); // или HideImmediate();
    }
    void MarkBadgesSeen()
    {
        if (currentDef == null) return;

        for (int i = 0; i < sessionBadges.Count; i++)
            RoomProgress.SetSeen(currentDef.id, sessionBadges[i], true);

        sessionBadges.Clear();
    }
}
