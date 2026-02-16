using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RewardPanel : MonoBehaviour
{
    [Header("Anim")]
    [SerializeField] private PopupAnimator animator;

    [Header("UI")]
    [SerializeField] private Button claimButton;
    [SerializeField] private Button claim2xButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Reward")]
    private int coinsReward = 50;
    [SerializeField] private int xpReward = 0;
    [SerializeField] private int scoreReward = 0;

    private Action onClosed;
    private bool shown;

    [SerializeField] private RectTransform coinSpawnPoint;
    public Action<Action<bool>> ShowRewardedAd;



    void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(() =>
            {
                AdController.Instance.ShowAd(() =>
                {
                    // после закрытия рекламы
                    ClaimAndClose(multiplier: 1);
                });
            });
        }
        if (claim2xButton != null)
        {
            claim2xButton.onClick.AddListener(() =>
            {
                AdController.Instance.ShowRewardAdv(
        onRewardFinished: () =>
        {
            Claim2xWithAd();
        },
        rewardType: RewardType.DoubleRewards,
        onClosedWithoutReward: () =>
        {
            // просто закрыли — ничего не делаем
        }
        );
            });
        }
        if (closeButton) closeButton.onClick.AddListener(Close);

        // чтобы на старте не торчала
        if (animator != null)
            animator.Hide();
    }

    public void Configure(int coins, int xp = 0, int score = 0, string title = null)
    {
        coinsReward = coins;
        xpReward = xp;
        scoreReward = score;

        if (titleText && !string.IsNullOrEmpty(title))
            titleText.text = title;

        if (rewardText)
        {
            // покажем красиво
            string s = "";
            if (coinsReward > 0) s += $"{coinsReward}";
            if (xpReward > 0) s += $"{xpReward}";
            if (scoreReward > 0) s += $"{scoreReward}";
            rewardText.text = s.Trim();
        }
    }

    public void Show(Action onClosed)
    {
        if (shown) return;
        shown = true;

        this.onClosed = onClosed;

        if (animator != null)
            animator.Show();
        else
            gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!shown) return;
        shown = false;

        if (animator != null)
            animator.Hide();
        else
            gameObject.SetActive(false);

        var cb = onClosed;
        onClosed = null;
        cb?.Invoke();
    }

    void ClaimAndClose(int multiplier)
    {
        if (claimButton) claimButton.interactable = false;
        if (claim2xButton) claim2xButton.interactable = false;

        int coins = coinsReward * multiplier;
        int xp = xpReward * multiplier;
        int score = scoreReward * multiplier;

        // 🔒 закрываем панель сразу
        Close();

        AudioManager.Instance?.Play("Success2");

        // 🚀 FX монет
        if (coins > 0 && CoinFlyDOTween.Instance != null && coinSpawnPoint != null)
        {
            CoinFlyDOTween.Instance.OnCoinArrived = null;
            CoinFlyDOTween.Instance.OnCoinArrived = () =>
            {
                GiveRewards(coins, xp, score);
                CoinFlyDOTween.Instance.OnCoinArrived = null;
            };

            CoinFlyDOTween.Instance.Play(coinSpawnPoint.anchoredPosition);
        }
        else
        {
            GiveRewards(coins, xp, score);
        }

        if (claimButton) claimButton.interactable = true;
        if (claim2xButton) claim2xButton.interactable = true;
    }
    void GiveRewards(int coins, int xp, int score)
    {
        if (PlayerProgress.Instance == null) return;

        if (coins != 0) PlayerProgress.Instance.AddCoinsAnimated(coins, 0.8f);
        if (score != 0) PlayerProgress.Instance.AddScore(score);
        if (xp != 0) PlayerProgress.Instance.AddXp(xp);
    }
    void Claim2xWithAd()
    {
        //if (ShowRewardedAd == null)
        //{
        //    Debug.LogWarning("RewardPanel: ShowRewardedAd is not assigned");
        //    return;
        //}

        //if (claimButton) claimButton.interactable = false;
        //if (claim2xButton) claim2xButton.interactable = false;
        //if (closeButton) closeButton.interactable = false;

        //ShowRewardedAd.Invoke(success =>
        //{
        //    // вернём интерактивность (на всякий)
        //    if (claimButton) claimButton.interactable = true;
        //    if (claim2xButton) claim2xButton.interactable = true;
        //    if (closeButton) closeButton.interactable = true;

        //    if (!shown) return; // если панель уже закрыли каким-то образом

        //    if (success)
        //        ClaimAndClose(multiplier: 2);
        //    else
        //        AudioManager.Instance?.Play("Click"); // или ничего
        //});
        // вернём интерактивность (на всякий)
        if (claimButton) claimButton.interactable = true;
        if (claim2xButton) claim2xButton.interactable = true;
        if (closeButton) closeButton.interactable = true;

        if (!shown) return; // если панель уже закрыли каким-то образом


        ClaimAndClose(multiplier: 2);
        AudioManager.Instance?.Play("Click"); // или ничего
    }
}
