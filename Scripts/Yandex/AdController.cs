using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public enum RewardType
{
    None,
    MultiplyReward,
    DoubleRewards,
    Continue,
    UnlockAndEquipItem,
    DoubleBooster
}

public class AdController : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ShowAdv();
    [DllImport("__Internal")] private static extern void ShowReward(string rewardType);
#endif

    public static AdController Instance;
    public bool IsFullScreenOpen => isFullScreenOpen;
    public bool IsRewardOpen => isRewardOpen;

    private Action onAdCallback;
    private Action onRewardCallback;
    private Action onRewardClosedCallback;

    private bool isAdRunning = false;
    private bool isFullScreenOpen = false;
    private bool isRewardOpen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void ShowAd(Action onAdFinished)
    {
        // ✅ No Ads куплено — просто продолжаем игру
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.NoAds)
        {
            Debug.Log("🚫 Fullscreen ad skipped (No Ads)");
            onAdFinished?.Invoke();
            return;
        }

        if (isAdRunning) { Debug.LogWarning("⚠️ Реклама уже запущена!"); return; }

        isAdRunning = true;
        isFullScreenOpen = true;
        onAdCallback = onAdFinished;

        PauseGame(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        ShowAdv();
#else
        StartCoroutine(FakeAdRoutine());
#endif
    }

    private IEnumerator FakeAdRoutine()
    {
        Debug.LogWarning("Запустил фулл скрин рекламу!");
        yield return new WaitForSecondsRealtime(2f);
        OnAdClosed();
    }

    private void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
    }

    public void OnAdClosed()
    {
        if (!isAdRunning || !isFullScreenOpen) return;

        isAdRunning = false;
        isFullScreenOpen = false;
        PauseGame(false);

        var cb = onAdCallback;
        onAdCallback = null;
        cb?.Invoke();
    }

    public void ShowRewardAdv(Action onRewardFinished, RewardType rewardType, Action onClosedWithoutReward = null)
    {
        if (isAdRunning) { Debug.LogWarning("⚠️ Реклама уже запущена!"); return; }

        isAdRunning = true;
        isRewardOpen = true;
        onRewardCallback = onRewardFinished;
        onRewardClosedCallback = onClosedWithoutReward;

        PauseGame(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        ShowReward(ToJsRewardType(rewardType));
#else
        StartCoroutine(FakeRewardRoutine(rewardType));
#endif
    }

    private string ToJsRewardType(RewardType t)
    {
        switch (t)
        {
            case RewardType.MultiplyReward: return "multiply_reward";
            case RewardType.DoubleRewards: return "double_rewards";
            case RewardType.Continue: return "continue";
            case RewardType.UnlockAndEquipItem: return "unlock_and_equip_item";
            case RewardType.DoubleBooster: return "double_booster";
            default: return "none";
        }
    }

    private IEnumerator FakeRewardRoutine(RewardType rewardType)
    {
        Debug.LogWarning("Запустил рекламу за ВОЗНАГРАЖДЕНИЕ");
        yield return new WaitForSecondsRealtime(2f);
        OnRewarded(rewardType);
    }

    public void OnRewarded(RewardType rewardType)
    {
        if (!isRewardOpen) return;

        isAdRunning = false;
        isRewardOpen = false;
        PauseGame(false);

        var cb = onRewardCallback;
        onRewardCallback = null;
        onRewardClosedCallback = null;

        cb?.Invoke();
    }

    public void OnRewardClosedWithoutReward()
    {
        if (!isRewardOpen) return;

        isAdRunning = false;
        isRewardOpen = false;
        PauseGame(false);

        var cb = onRewardClosedCallback;
        onRewardCallback = null;
        onRewardClosedCallback = null;

        cb?.Invoke();
    }
}
