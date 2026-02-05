using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameFlow
{
    const string K_GAME_LEVEL = "game_level"; // уровень игры: 1,2,3...
    public static event Action<int> OnGameLevelChanged;
    public static int GameLevel
    {
        get => PlayerPrefs.GetInt(K_GAME_LEVEL, 1);
        set
        {
            int v = Mathf.Max(1, value);
            int old = GameLevel;
            if (old == v) return;

            PlayerPrefs.SetInt(K_GAME_LEVEL, v);
            PlayerPrefs.Save();
            OnGameLevelChanged?.Invoke(v);
        }
    }

    public static bool IsOnboarding => GameLevel <= 3;

    public static void AdvanceGameLevel()
    {
        GameLevel = GameLevel + 1;
    }

    public static void ResetRun()
    {
        GameLevel = 1;
    }
}
