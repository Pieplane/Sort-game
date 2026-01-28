using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameFlow
{
    const string K_GAME_LEVEL = "game_level"; // уровень игры: 1,2,3...
    public static int GameLevel
    {
        get => PlayerPrefs.GetInt(K_GAME_LEVEL, 1);
        set { PlayerPrefs.SetInt(K_GAME_LEVEL, Mathf.Max(1, value)); PlayerPrefs.Save(); }
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
