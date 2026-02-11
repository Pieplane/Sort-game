using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoAdsOfferManager : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private int minLevels = 3;
    [SerializeField] private int minRankToOffer = 4;
    [SerializeField] private int adsSeenToQualify = 2; // сейчас не используется (как у тебя было)
    [SerializeField] private int cooldownHours = 24;

    [Header("UI")]
    [SerializeField] private PopupAnimator offerPopup;

    // локальные (не обязательно в облако)
    const string K_PendingOffer = "NOADS_PENDING";
    const string K_PendingReason = "NOADS_PENDING_REASON";

    bool isShowing;
    [SerializeField] private bool debug = true;

    NoAdsOfferSaveData Data
    {
        get
        {
            var pp = PlayerProgress.Instance;
            if (pp == null) return null;
            if (pp.NoAdsOffer == null) pp.SaveNoAdsOffer(new NoAdsOfferSaveData());
            return pp.NoAdsOffer;
        }
    }

    // ---------- Public API ----------
    public void RegisterLevelWon()
    {
        if (!EnsureReady()) return;

        Data.levelsWon++;
        SaveCloud();

        if (debug)
            Debug.Log($"[NoAds] Level WON registered. Total levels = {Data.levelsWon}");
    }

    public void RegisterAdSeen()
    {
        if (!EnsureReady()) return;

        Data.adsSeen++;
        SaveCloud();

        if (debug)
            Debug.Log($"[NoAds] Ad SEEN registered. Ads seen = {Data.adsSeen}");
    }

    public void RegisterContinueUsed()
    {
        if (!EnsureReady()) return;

        Data.continueUsed++;
        SaveCloud();

        if (debug)
            Debug.Log($"[NoAds] Continue USED. Total continues = {Data.continueUsed}");
    }

    /// <summary>Попросить показать оффер в безопасный момент (например после Win/Lose).</summary>
    public void TryShowOffer(string reason = "")
    {
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.NoAds)
            return;

        if (isShowing)
        {
            if (debug) Debug.Log("[NoAds] Offer already showing, skip");
            return;
        }

        if (offerPopup == null)
        {
            Debug.LogWarning("[NoAds] Offer popup is NULL");
            return;
        }

        if (!CanShowNow())
        {
            if (debug) Debug.Log($"[NoAds] Offer NOT shown. Reason: {reason}");
            return;
        }

        MarkShownNow();
        isShowing = true;

        if (debug)
            Debug.Log($"[NoAds] 🎉 Showing NO ADS OFFER. Reason = {reason}");

        offerPopup.Show();
    }

    public void CloseOffer()
    {
        if (debug)
            Debug.Log("[NoAds] Offer closed by user");

        AudioManager.Instance?.Play("Click");

        if (offerPopup != null)
            offerPopup.Hide();

        isShowing = false;
    }

    // ---------- Core logic ----------
    bool CanShowNow()
    {
        if (!EnsureReady()) return false;

        // ✅ уже купили noads -> оффер больше никогда не показываем
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.NoAds)
        {
            if (debug) Debug.Log("[NoAds] ✅ NoAds already owned -> offer disabled");
            return false;
        }

        int playerRank = (PlayerProgress.Instance != null) ? PlayerProgress.Instance.Level : 1;

        int today = TodayKeyUtc();
        int lastDay = Data.lastOfferDay;

        double hoursSinceLast = Data.lastOfferUtcTicks > 0
            ? (DateTime.UtcNow - new DateTime(Data.lastOfferUtcTicks, DateTimeKind.Utc)).TotalHours
            : double.MaxValue;

        if (debug)
        {
            Debug.Log(
                $"[NoAds] CanShowNow check:\n" +
                $"- Rank: {playerRank}/{minRankToOffer}\n" +
                $"- LevelsWon: {Data.levelsWon}/{minLevels}\n" +
                $"- AdsSeen: {Data.adsSeen} (need {adsSeenToQualify})\n" +
                $"- ContinueUsed: {Data.continueUsed}\n" +
                $"- LastDay: {lastDay}, Today: {today}\n" +
                $"- Hours since last offer: {hoursSinceLast:F1}/{cooldownHours}"
            );
        }

        if (playerRank < minRankToOffer)
        {
            if (debug) Debug.Log("[NoAds] ❌ Rank too low for offer");
            return false;
        }

        if (Data.levelsWon < minLevels)
        {
            if (debug) Debug.Log("[NoAds] ❌ Not enough levels won");
            return false;
        }

        // если захочешь вернуть условие — просто раскомментируй
        // if (!(Data.adsSeen >= adsSeenToQualify || Data.continueUsed >= 1)) return false;

        if (lastDay == today)
        {
            if (debug) Debug.Log("[NoAds] ❌ Already shown today");
            return false;
        }

        if (hoursSinceLast < cooldownHours)
        {
            if (debug) Debug.Log("[NoAds] ❌ Cooldown not passed");
            return false;
        }

        if (debug) Debug.Log("[NoAds] ✅ All conditions met. Can show offer!");
        return true;
    }

    void MarkShownNow()
    {
        if (!EnsureReady()) return;

        Data.lastOfferDay = TodayKeyUtc();
        Data.lastOfferUtcTicks = DateTime.UtcNow.Ticks;

        SaveCloud();
    }

    public void RequestOfferNextScene(string reason = "")
    {
        if (PlayerProgress.Instance != null &&
        PlayerProgress.Instance.NoAds)
            return;

        if (!CanShowNow()) return;

        // ✅ pending оставляем локально
        PlayerPrefs.SetInt(K_PendingOffer, 1);
        PlayerPrefs.SetString(K_PendingReason, reason);
        PlayerPrefs.Save();

        if (debug) Debug.Log($"[NoAds] Offer requested for next scene. Reason={reason}");
    }

    public void TryShowPendingOffer()
    {
        if (PlayerPrefs.GetInt(K_PendingOffer, 0) != 1) return;

        string reason = PlayerPrefs.GetString(K_PendingReason, "");
        PlayerPrefs.SetInt(K_PendingOffer, 0);
        PlayerPrefs.SetString(K_PendingReason, "");
        PlayerPrefs.Save();

        TryShowOffer(reason);
    }

    // ---------- Helpers ----------
    bool EnsureReady()
    {
        if (PlayerProgress.Instance == null) return false;
        if (!PlayerProgress.Instance.IsLoaded) return false; // важно для WebGL async
        if (PlayerProgress.Instance.NoAdsOffer == null)
            PlayerProgress.Instance.SaveNoAdsOffer(new NoAdsOfferSaveData());
        return true;
    }

    void SaveCloud()
    {
        // сохраняем через общий прогресс (в Editor -> PlayerPrefs, в WebGL -> Cloud)
        PlayerProgress.Instance?.SaveNoAdsOffer(Data);
    }

    static int TodayKeyUtc()
    {
        var d = DateTime.UtcNow;
        return d.Year * 10000 + d.Month * 100 + d.Day;
    }
    //public void DisableForever()
    //{
    //    // закрыть если открыт
    //    if (offerPopup != null) offerPopup.Hide();
    //    isShowing = false;

    //    // убрать pending
    //    PlayerPrefs.SetInt(K_PendingOffer, 0);
    //    PlayerPrefs.SetString(K_PendingReason, "");
    //    PlayerPrefs.Save();

    //    if (debug) Debug.Log("[NoAds] Offer disabled forever (NoAds purchased)");
    //}
}
