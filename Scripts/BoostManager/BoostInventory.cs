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
    public static event Action<BoostType, int> OnBoostChanged;

    public static int Get(BoostType type)
    {
        var pp = PlayerProgress.Instance;
        return pp != null ? pp.GetBoost(type) : 0;
    }

    public static void Set(BoostType type, int value)
    {
        var pp = PlayerProgress.Instance;
        if (pp == null) return;

        pp.SetBoost(type, value);
        OnBoostChanged?.Invoke(type, pp.GetBoost(type));
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
