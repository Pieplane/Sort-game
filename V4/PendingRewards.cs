using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PendingRewardsData
{
    public int coins;
    public int score;
    public int xp;
    public int multiplier;
    public int stars;
    public int sourceLevel; // какой уровень (игровой) дал награду
}
public static class PendingRewards
{
    const string K = "pending_rewards_v1";

    public static void Set(PendingRewardsData data)
    {
        PlayerPrefs.SetString(K, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryGet(out PendingRewardsData data)
    {
        if (!PlayerPrefs.HasKey(K))
        {
            data = default;
            return false;
        }
        data = JsonUtility.FromJson<PendingRewardsData>(PlayerPrefs.GetString(K));
        return true;
    }

    public static void Clear()
    {
        if (PlayerPrefs.HasKey(K)) PlayerPrefs.DeleteKey(K);
    }
}
