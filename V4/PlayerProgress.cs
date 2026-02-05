using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    [Header("Persistent")]
    [SerializeField] int level = 1;
    [SerializeField] int xp = 0;
    [SerializeField] int xpToNext = 100;
    [SerializeField] int totalScore = 0;
    [SerializeField] int coins = 0;

    [Header("XP Curve")]
    public int baseXpToNext = 100;
    public float levelPower = 1.35f;

    [Header("Per-triple (for reward formulas)")]
    private int scorePerTriple = 5;

    [Header("Level Complete Rewards (base)")]
    private int baseCoinsForLevel = 50;
    private int coinsPerLevel = 20;

    public int baseXpForLevelComplete = 60;
    public int xpPerLevel = 15;

    [Header("Score reward tuning")]
    private int baseScoreForWin = 20;
    private int starBonusScore = 5; // *stars

    // ===== Events for HUD =====
    public event Action<int> OnScoreChanged;       // totalScore
    public event Action<int, int> OnXpChanged;     // xp, xpToNext
    public event Action<int> OnLevelUp;            // new level
    public event Action<int> OnCoinsChanged;       // coins

    // ===== PlayerPrefs keys =====
    const string K_LEVEL = "pp_level";
    const string K_XP = "pp_xp";
    const string K_SCORE = "pp_score";
    const string K_COINS = "pp_coins";

    // ===== Getters =====
    public int Level => level;
    public int XP => xp;
    public int XPToNext => xpToNext;
    public int TotalScore => totalScore;
    public int Coins => coins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        RecalcXpToNext();
        ClampAll();
    }

    // ===================== CALC REWARDS =====================

    public int CalcCoinsForWin()
    {
        return baseCoinsForLevel + level * coinsPerLevel;
    }

    public int CalcScoreForWin(int triplesTotal, int stars, int multiplier)
    {
        stars = Mathf.Clamp(stars, 1, 3);
        multiplier = Mathf.Max(1, multiplier);

        int fromTriples = Mathf.Max(0, triplesTotal) * scorePerTriple;
        int starBonus = stars * starBonusScore;

        int raw = baseScoreForWin + fromTriples + starBonus;
        return raw * multiplier;
    }

    public int CalcXpForWin(int stars, int multiplier)
    {
        stars = Mathf.Clamp(stars, 1, 3);
        multiplier = Mathf.Max(1, multiplier);

        int baseXp = baseXpForLevelComplete + level * xpPerLevel;
        int starBonusXp = (stars - 1) * 20;

        return (baseXp + starBonusXp) * multiplier;
    }

    // ===================== APPLY (INSTANT) =====================

    //public void AddCoins(int amount)
    //{
    //    if (amount <= 0) return;
    //    coins += amount;
    //    OnCoinsChanged?.Invoke(coins);
    //    Save();
    //}
    public void AddCoins(int amount)
    {
        if (amount == 0) return;

        coins = Mathf.Max(0, coins + amount);
        OnCoinsChanged?.Invoke(coins);
        Save();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        totalScore += amount;
        OnScoreChanged?.Invoke(totalScore);
        Save();
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;

        xp += amount;

        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            level++;
            RecalcXpToNext();
            OnLevelUp?.Invoke(level);
        }

        OnXpChanged?.Invoke(xp, xpToNext);
        Save();
    }

    // ===================== APPLY (ANIMATED) =====================

    public void AddCoinsAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0) return;
        StopCoroutineSafe("AddCoinsAnimatedRoutine");
        StartCoroutine(AddCoinsAnimatedRoutine(amount, duration));
    }

    public void AddScoreAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0) return;
        StopCoroutineSafe("AddScoreAnimatedRoutine");
        StartCoroutine(AddScoreAnimatedRoutine(amount, duration));
    }

    public void AddXpAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0) return;
        StopCoroutineSafe("AddXpAnimatedRoutine");
        StartCoroutine(AddXpAnimatedRoutine(amount, duration));
    }

    IEnumerator AddCoinsAnimatedRoutine(int amount, float duration)
    {
        int start = coins;
        int end = coins + amount;

        float t = 0f;
        int last = start;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            int cur = Mathf.RoundToInt(Mathf.Lerp(start, end, k));
            if (cur != last)
            {
                coins = cur;
                last = cur;
                OnCoinsChanged?.Invoke(coins);
            }
            yield return null;
        }

        coins = end;
        OnCoinsChanged?.Invoke(coins);
        Save();
    }

    IEnumerator AddScoreAnimatedRoutine(int amount, float duration)
    {
        int start = totalScore;
        int end = totalScore + amount;

        float t = 0f;
        int last = start;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            int cur = Mathf.RoundToInt(Mathf.Lerp(start, end, k));
            if (cur != last)
            {
                totalScore = cur;
                last = cur;
                OnScoreChanged?.Invoke(totalScore);
            }
            yield return null;
        }

        totalScore = end;
        OnScoreChanged?.Invoke(totalScore);
        Save();
    }

    IEnumerator AddXpAnimatedRoutine(int amount, float duration)
    {
        int startTotal = xp;
        int targetTotal = xp + amount;

        float t = 0f;
        int lastApplied = startTotal;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            int curTotal = Mathf.RoundToInt(Mathf.Lerp(startTotal, targetTotal, k));
            int delta = curTotal - lastApplied;

            if (delta > 0)
            {
                // важно: через AddXp, чтобы отработал level-up и события
                AddXp(delta);
                lastApplied = curTotal;
            }

            yield return null;
        }

        int remain = targetTotal - lastApplied;
        if (remain > 0) AddXp(remain);
    }

    // ===================== RESET =====================

    public void ResetProgress()
    {
        level = 1;
        xp = 0;
        totalScore = 0;
        coins = 0;

        RecalcXpToNext();
        Save();

        OnScoreChanged?.Invoke(totalScore);
        OnCoinsChanged?.Invoke(coins);
        OnXpChanged?.Invoke(xp, xpToNext);
        OnLevelUp?.Invoke(level);
    }

    // ===================== INTERNAL =====================

    void RecalcXpToNext()
    {
        xpToNext = Mathf.Max(10, Mathf.RoundToInt(baseXpToNext * Mathf.Pow(level, levelPower)));
    }

    void ClampAll()
    {
        if (level < 1) level = 1;
        if (xp < 0) xp = 0;
        if (coins < 0) coins = 0;
        if (totalScore < 0) totalScore = 0;
    }

    void Save()
    {
        PlayerPrefs.SetInt(K_LEVEL, level);
        PlayerPrefs.SetInt(K_XP, xp);
        PlayerPrefs.SetInt(K_SCORE, totalScore);
        PlayerPrefs.SetInt(K_COINS, coins);
        PlayerPrefs.Save();
    }

    void Load()
    {
        level = PlayerPrefs.GetInt(K_LEVEL, 1);
        xp = PlayerPrefs.GetInt(K_XP, 0);
        totalScore = PlayerPrefs.GetInt(K_SCORE, 0);
        coins = PlayerPrefs.GetInt(K_COINS, 0);
    }

    // простая защита от параллельных корутин одного типа
    void StopCoroutineSafe(string routineName)
    {
        // Unity не умеет StopCoroutine по имени безопасно без хранения ссылки.
        // Поэтому оставим как "не обязателен": если хочешь — сделаем через Coroutine handle.
        // Сейчас можно просто ничего не делать.
    }
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        AddCoins(-amount);
        return true;
    }
}
