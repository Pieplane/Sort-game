using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adv : MonoBehaviour
{
    public static Adv Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // FULLSCREEN -------------------

    public void OnOpen()
    {
        AudioListener.volume = 0f;
        Debug.Log("🔇 Audio muted (interstitial open)");
    }

    public void OnClose()
    {
        AudioListener.volume = 1f;
        Debug.Log("🎧 Audio unmuted (interstitial close)");

        // ✅ сообщаем контроллеру, что реклама закрылась
        AdController.Instance?.OnAdClosed();
    }

    public void OnError()
    {
        Debug.LogWarning("⚠️ Interstitial error");
        OnClose(); // считаем как закрытие
    }

    public void OnOffline()
    {
        Debug.LogWarning("📴 Interstitial offline");
        OnClose();
    }

    // REWARD -----------------------

    public void OnOpenReward()
    {
        AudioListener.volume = 0f;
        Debug.Log("🔇 Audio muted (reward open)");
    }

    // rewardType приходит из JS строкой (например "continue" или "double_rewards")
    public void OnRewarded(string rewardType)
    {
        Debug.Log($"🏆 Reward received: {rewardType}");

        AudioListener.volume = 1f;

        // ✅ важно: сообщаем AdController, что награда получена
        // AdController у тебя ждёт RewardType enum — сделаем разбор строки
        var parsed = ParseRewardType(rewardType);
        AdController.Instance?.OnRewarded(parsed);
    }

    public void OnCloseReward()
    {
        Debug.Log("❌ Reward ad closed without reward");

        AudioListener.volume = 1f;

        // ✅ сообщаем контроллеру "закрыли без награды"
        AdController.Instance?.OnRewardClosedWithoutReward();
    }

    public void OnErrorReward()
    {
        Debug.LogWarning("⚠️ Reward ad error");
        OnCloseReward(); // считаем как закрытие без награды
    }

    private RewardType ParseRewardType(string s)
    {
        // JS может слать либо "double_rewards" либо "DoubleRewards" — подстрахуемся
        if (string.IsNullOrEmpty(s)) return RewardType.None;
        s = s.Trim().ToLowerInvariant();

        switch (s)
        {
            case "multiply_reward": return RewardType.MultiplyReward;
            case "double_rewards": return RewardType.DoubleRewards;
            case "continue": return RewardType.Continue;
            case "unlock_and_equip_item": return RewardType.UnlockAndEquipItem;
            case "double_booster": return RewardType.DoubleBooster;
            default: return RewardType.None;
        }
    }
}
