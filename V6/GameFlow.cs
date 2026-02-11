using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameFlow
{
    public static event Action<int> OnGameLevelChanged;

    public static int GameLevel =>
        PlayerProgress.Instance != null ? PlayerProgress.Instance.GameLevel : 1;

    public static bool IsOnboarding => GameLevel <= 3;

    public static void AdvanceGameLevel()
    {
        PlayerProgress.Instance?.AdvanceGameLevel();
    }

    public static void ResetRun()
    {
        PlayerProgress.Instance?.ResetRun();
    }

    // проброс события
    public static void BindToProgress()
    {
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.OnGameLevelChanged += lvl =>
        {
            OnGameLevelChanged?.Invoke(lvl);
        };
    }
}
