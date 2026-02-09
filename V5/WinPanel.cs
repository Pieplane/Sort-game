using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup rootGroup; // на WinPanel
    [SerializeField] private RectTransform panel;   // окно
    [SerializeField] private Image dim;             // затемнение
    [SerializeField] private Image[] stars;         // 3 звезды слева направо

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI rewardText; // coins
    [SerializeField] private TextMeshProUGUI xpText;     // score/exp
    [SerializeField] private TextMeshProUGUI multText;   // "x2"
    [SerializeField] private TextMeshProUGUI bonusText;  // "+120"

    [Header("Pop targets (optional)")]
    [SerializeField] private RectTransform rewardPop;
    [SerializeField] private RectTransform xpPop;

    [Header("Anim")]
    [SerializeField] private float dimTargetAlpha = 0.55f;
    [SerializeField] private float panelInDur = 0.35f;
    [SerializeField] private float starDelay = 0.12f;

    [Header("Multiplier Anim")]
    [SerializeField] private float numberAnimDur = 0.45f;
    [SerializeField] private float popScale = 1.15f;

    // ===== tweens we control =====
    private Sequence showSeq;
    private Sequence bonusSeq;
    private Tween coinsTween;
    private Tween xpTween;
    private Tween popCoinsTween;
    private Tween popXpTween;

    // базовые награды (до множителя)
    int coinsBase;
    int xpBase;

    // текущие (после множителя тоже сюда)
    int coinsCurrent;
    int xpCurrent;

    void Reset()
    {
        rootGroup = GetComponent<CanvasGroup>();
        panel = transform.Find("Panel") as RectTransform;
    }

    void OnDisable()
    {
        KillAllTweens();
    }

    void OnDestroy()
    {
        KillAllTweens();
    }

    void KillAllTweens()
    {
        showSeq?.Kill(); showSeq = null;
        bonusSeq?.Kill(); bonusSeq = null;

        coinsTween?.Kill(); coinsTween = null;
        xpTween?.Kill(); xpTween = null;

        popCoinsTween?.Kill(); popCoinsTween = null;
        popXpTween?.Kill(); popXpTween = null;

        // на всякий — убьём твины на ключевых объектах
        if (panel) panel.DOKill();
        if (dim) dim.DOKill();

        if (rewardText) rewardText.DOKill();
        if (xpText) xpText.DOKill();
        if (multText) multText.DOKill();
        if (bonusText) bonusText.DOKill();

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
                if (stars[i]) stars[i].DOKill();
        }
    }
    

    //IEnumerator DelayedCall(float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    if (AudioManager.Instance != null)
    //    {
    //        // ================================== 🎧 AUDIO MANAGER CALL ==================================
    //        AudioManager.Instance.Play("Yay");
    //    }
    //}

    public void Show(int starsCount, int coinsReward, int xpReward)
    {
        gameObject.SetActive(true);

        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Success");
        }
        //StartCoroutine(DelayedCall(0.6f));

        // сохранить базу
        coinsBase = coinsReward;
        xpBase = xpReward;
        coinsCurrent = coinsReward;
        xpCurrent = xpReward;

        KillAllTweens(); // ✅ важнее, чем Kill(panel) отдельно

        // базовое состояние
        if (rootGroup)
        {
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
        }

        if (dim)
        {
            var c = dim.color;
            dim.color = new Color(c.r, c.g, c.b, 0f);
        }

        float startY = 0f;
        if (panel)
        {
            panel.localScale = Vector3.one * 0.75f;
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, panel.anchoredPosition.y - 30f);
            startY = panel.anchoredPosition.y;
        }

        // звёзды прячем
        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (!stars[i]) continue;
                stars[i].gameObject.SetActive(false);
                stars[i].transform.localScale = Vector3.zero;
                var col = stars[i].color;
                stars[i].color = new Color(col.r, col.g, col.b, 1f);
            }
        }

        if (rewardText) rewardText.text = $"{coinsReward}";
        if (xpText) xpText.text = $"{xpReward}";

        if (multText) multText.text = "x1";
        if (bonusText) bonusText.gameObject.SetActive(false);

        // SEQUENCE
        showSeq = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy); // ✅ ключ

        // 1) затемнение
        if (dim) showSeq.Join(dim.DOFade(dimTargetAlpha, 0.25f).SetEase(Ease.OutQuad).SetLink(gameObject));

        // 2) окно
        if (panel)
        {
            showSeq.Join(panel.DOAnchorPosY(startY + 30f, panelInDur).SetEase(Ease.OutCubic).SetLink(gameObject));
            showSeq.Join(panel.DOScale(1f, panelInDur).SetEase(Ease.OutBack, 1.6f).SetLink(gameObject));
        }

        // 3) звёзды
        starsCount = Mathf.Clamp(starsCount, 0, stars != null ? stars.Length : 0);

        for (int i = 0; i < starsCount; i++)
        {
            int idx = i;
            if (stars == null || stars[idx] == null) continue;

            showSeq.AppendInterval(starDelay);

            showSeq.AppendCallback(() =>
            {
                // ✅ защита от смены сцены
                if (!this) return;
                if (stars == null || idx < 0 || idx >= stars.Length) return;
                if (stars[idx] == null) return;

                stars[idx].gameObject.SetActive(true);
            });

            showSeq.Append(stars[idx].transform
                .DOScale(1f, 0.22f)
                .SetEase(Ease.OutBack, 2.2f)
                .SetLink(gameObject));

            showSeq.Join(stars[idx].transform
                .DOScale(1.08f, 0.10f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject));
        }
    }

    public (int coinsFinal, int xpFinal) ApplyMultiplier(int m)
    {
        m = Mathf.Max(1, m);

        int coinsFinal = coinsBase * m;
        int xpFinal = xpBase * m;

        int bonusCoins = coinsFinal - coinsCurrent;
        int bonusXp = xpFinal - xpCurrent;

        if (multText) multText.text = $"x{m}";

        // ✅ убиваем прошлые числовые твины
        coinsTween?.Kill();
        xpTween?.Kill();

        coinsTween = AnimateInt(coinsCurrent, coinsFinal, numberAnimDur, v =>
        {
            // ✅ защита на случай уничтожения
            if (!this) return;
            coinsCurrent = v;
            if (rewardText) rewardText.text = v.ToString();
        });

        popCoinsTween?.Kill();
        popCoinsTween = Pop(rewardPop ? rewardPop : (rewardText ? rewardText.rectTransform : null));

        xpTween = AnimateInt(xpCurrent, xpFinal, numberAnimDur, v =>
        {
            if (!this) return;
            xpCurrent = v;
            if (xpText) xpText.text = v.ToString();
        });

        popXpTween?.Kill();
        popXpTween = Pop(xpPop ? xpPop : (xpText ? xpText.rectTransform : null));

        // бонус
        if (bonusText)
        {
            bonusSeq?.Kill();

            bonusText.gameObject.SetActive(true);
            bonusText.alpha = 0f;
            bonusText.rectTransform.localScale = Vector3.one * 0.85f;
            bonusText.text = $"+{bonusCoins} / +{bonusXp}";

            bonusSeq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            bonusSeq.Append(bonusText.DOFade(1f, 0.12f).SetLink(gameObject));
            bonusSeq.Join(bonusText.rectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetLink(gameObject));
            bonusSeq.AppendInterval(0.8f);
            bonusSeq.Append(bonusText.DOFade(0f, 0.2f).SetLink(gameObject));
            bonusSeq.OnComplete(() =>
            {
                if (!this || bonusText == null) return;
                bonusText.gameObject.SetActive(false);
            });
        }

        return (coinsFinal, xpFinal);
    }

    Tween AnimateInt(int from, int to, float dur, System.Action<int> onValue)
    {
        int v = from;
        return DOTween.To(() => v, x =>
        {
            v = x;
            onValue?.Invoke(x);
        }, to, dur)
        .SetEase(Ease.OutCubic)
        .SetUpdate(true)
        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    Tween Pop(RectTransform rt)
    {
        if (!rt) return null;

        rt.DOKill();
        rt.localScale = Vector3.one;

        // Один tween, чтобы его можно было хранить/убивать
        return rt.DOScale(popScale, 0.12f).SetEase(Ease.OutBack).SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(() =>
            {
                if (!this || rt == null) return;
                rt.DOScale(1f, 0.10f).SetEase(Ease.OutSine).SetUpdate(true)
                  .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            });
    }

    public void Hide()
    {
        KillAllTweens();          // ✅ сначала убить всё
        gameObject.SetActive(false);
        
    }

    public void SetRewardTexts(int coins, int score, int mult)
    {
        if (rewardText) rewardText.text = $"{coins}";
        if (xpText) xpText.text = $"{score}";

        if (rewardText) rewardText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 8, 0.8f)
            .SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (xpText) xpText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 8, 0.8f)
            .SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void SetInteractable(bool value)
    {
        if (rootGroup == null) return;
        rootGroup.interactable = value;
        rootGroup.blocksRaycasts = value;
    }
}
