using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoostType
{
    ClearSomeOfType,
    ShuffleButton,
    HintThreeSame
}
public class BoostInventory : MonoBehaviour
{
    const string KeyPrefix = "boost_";

    static readonly Dictionary<BoostType, int> cache = new();

    public static event Action<BoostType, int> OnBoostChanged;

    public static int Get(BoostType type)
    {
        if (cache.TryGetValue(type, out int v)) return v;
        int loaded = PlayerPrefs.GetInt(KeyPrefix + type, 0);
        cache[type] = loaded;
        return loaded;
    }

    public static void Set(BoostType type, int value)
    {
        value = Mathf.Max(0, value);
        cache[type] = value;

        PlayerPrefs.SetInt(KeyPrefix + type, value);
        PlayerPrefs.Save();

        OnBoostChanged?.Invoke(type, value);
    }

    public static void Add(BoostType type, int delta)
    {
        Set(type, Get(type) + delta);
    }

    public static bool TryConsume(BoostType type, int amount = 1)
    {
        int cur = Get(type);
        if (cur < amount) return false;
        Set(type, cur - amount);
        return true;
    }
}
