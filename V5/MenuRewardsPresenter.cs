using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuRewardsPresenter : MonoBehaviour
{
    [Header("Spawns")]
    [SerializeField] private RectTransform coinsSpawn;
    [SerializeField] private RectTransform scoreSpawn;
    [SerializeField] private RectTransform xpSpawn;

    [Header("Timings")]
    [SerializeField] private float coinsDelay = 0.0f;
    [SerializeField] private float scoreDelay = 0.6f;
    [SerializeField] private float xpDelay = 1.2f;

    [SerializeField] private float coinsAnimTime = 0.8f;
    [SerializeField] private float scoreAnimTime = 0.9f;
    [SerializeField] private float xpAnimTime = 0.9f;

    [SerializeField] private NoAdsOfferManager noAdsOffer;

    void Start()
    {
        noAdsOffer?.TryShowPendingOffer();

        if (!PendingRewards.TryGet(out var data))
            return;

        StartCoroutine(PlayRewardsRoutine(data));
    }
    void OnDisable()
    {
        DOTween.Kill(gameObject);   // убьёт все твины, залинкованные на этот GO
    }

    IEnumerator PlayRewardsRoutine(PendingRewardsData data)
    {
        // ===== COINS =====
        yield return new WaitForSecondsRealtime(coinsDelay);

        if (CoinFlyDOTween.Instance != null && coinsSpawn != null)
            CoinFlyDOTween.Instance.Play(coinsSpawn.anchoredPosition);

        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.AddCoinsAnimated(data.coins, coinsAnimTime);

        // ===== SCORE =====
        yield return new WaitForSecondsRealtime(scoreDelay);

        if (XpFlyDOTween.Instance != null && scoreSpawn != null)
            XpFlyDOTween.Instance.Play(scoreSpawn.anchoredPosition);

        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.AddScoreAnimated(data.score, scoreAnimTime);

        // ===== XP =====
        yield return new WaitForSecondsRealtime(xpDelay);

        if (XpFlyDOTween.Instance != null && xpSpawn != null)
            XpFlyDOTween.Instance.Play(xpSpawn.anchoredPosition);

        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.AddXpAnimated(data.xp, xpAnimTime);

        // ===== DONE =====
        PendingRewards.Clear();
    }
}
