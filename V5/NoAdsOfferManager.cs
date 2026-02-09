using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoAdsOfferManager : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private int minLevels = 3;
    [SerializeField] private int adsSeenToQualify = 2;
    [SerializeField] private int cooldownHours = 24;

    [Header("UI")]
    [SerializeField] private PopupAnimator offerPopup; // твой аниматор/панель оффера

    // PlayerPrefs keys
    const string K_LevelsWon = "NOADS_LEVELS_WON";
    const string K_AdsSeen = "NOADS_ADS_SEEN";
    const string K_ContinueUsed = "NOADS_CONTINUE_USED";
    const string K_LastOfferUtc = "NOADS_LAST_OFFER_UTC";
    const string K_LastOfferDay = "NOADS_LAST_OFFER_DAY"; // yyyymmdd

    const string K_PendingOffer = "NOADS_PENDING";
    const string K_PendingReason = "NOADS_PENDING_REASON";

    bool isShowing;

    [SerializeField] private bool debug = true;

    // ---------- Public API: вызывать из игры ----------
    public void RegisterLevelWon()
    {
        int newValue = GetInt(K_LevelsWon) + 1;
        PlayerPrefs.SetInt(K_LevelsWon, newValue);
        PlayerPrefs.Save();

        if (debug)
            Debug.Log($"[NoAds] Level WON registered. Total levels = {newValue}");
    }

    public void RegisterAdSeen()
    {
        int newValue = GetInt(K_AdsSeen) + 1;
        PlayerPrefs.SetInt(K_AdsSeen, newValue);
        PlayerPrefs.Save();

        if (debug)
            Debug.Log($"[NoAds] Ad SEEN registered. Ads seen = {newValue}");
    }

    public void RegisterContinueUsed()
    {
        int newValue = GetInt(K_ContinueUsed) + 1;
        PlayerPrefs.SetInt(K_ContinueUsed, newValue);
        PlayerPrefs.Save();

        if (debug)
            Debug.Log($"[NoAds] Continue USED. Total continues = {newValue}");
    }

    /// <summary>
    /// Попросить показать оффер в безопасный момент (например после Win/Lose).
    /// </summary>
    public void TryShowOffer(string reason = "")
    {
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

        if (offerPopup != null)
            offerPopup.Hide();

        isShowing = false;
    }

    // ---------- Core logic ----------
    bool CanShowNow()
    {
        int levelsWon = GetInt(K_LevelsWon);
        int adsSeen = GetInt(K_AdsSeen);
        int cont = GetInt(K_ContinueUsed);

        int today = TodayKeyUtc();
        int lastDay = GetInt(K_LastOfferDay, -1);

        long lastUtcTicks = GetLong(K_LastOfferUtc, 0);
        double hoursSinceLast = lastUtcTicks > 0
            ? (DateTime.UtcNow - new DateTime(lastUtcTicks, DateTimeKind.Utc)).TotalHours
            : double.MaxValue;

        if (debug)
        {
            Debug.Log(
                $"[NoAds] CanShowNow check:\n" +
                $"- LevelsWon: {levelsWon}/{minLevels}\n" +
                $"- AdsSeen: {adsSeen} (need {adsSeenToQualify})\n" +
                $"- ContinueUsed: {cont}\n" +
                $"- LastDay: {lastDay}, Today: {today}\n" +
                $"- Hours since last offer: {hoursSinceLast:F1}/{cooldownHours}"
            );
        }

        if (levelsWon < minLevels)
        {
            if (debug) Debug.Log("[NoAds] ❌ Not enough levels won");
            return false;
        }

        //if (!(adsSeen >= adsSeenToQualify || cont >= 1))
        //{
        //    if (debug) Debug.Log("[NoAds] ❌ Not enough ads seen or continues used");
        //    return false;
        //}

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
        PlayerPrefs.SetInt(K_LastOfferDay, TodayKeyUtc());
        PlayerPrefs.SetString(K_LastOfferUtc, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    static int TodayKeyUtc()
    {
        var d = DateTime.UtcNow;
        return d.Year * 10000 + d.Month * 100 + d.Day; // yyyymmdd
    }

    static int GetInt(string key, int def = 0)
    {
        return PlayerPrefs.GetInt(key, def);
    }

    static long GetLong(string key, long def = 0)
    {
        var s = PlayerPrefs.GetString(key, "");
        if (long.TryParse(s, out var v)) return v;
        return def;
    }
    public void RequestOfferNextScene(string reason = "")
    {
        if (!CanShowNow()) return;

        // Важно: НЕ MarkShownNow() тут, иначе ты "съешь" показ, если сцена не успела показать
        PlayerPrefs.SetInt(K_PendingOffer, 1);
        PlayerPrefs.SetString(K_PendingReason, reason);
        PlayerPrefs.Save();

        if (debug) Debug.Log($"[NoAds] Offer requested for next scene. Reason={reason}");
    }

    public void TryShowPendingOffer()
    {
        if (GetInt(K_PendingOffer, 0) != 1) return;

        string reason = PlayerPrefs.GetString(K_PendingReason, "");
        PlayerPrefs.SetInt(K_PendingOffer, 0);
        PlayerPrefs.SetString(K_PendingReason, "");
        PlayerPrefs.Save();

        // Теперь реально показываем
        TryShowOffer(reason);
    }
}
