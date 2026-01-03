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

    [Header("XP Curve")]
    public int baseXpToNext = 100;
    public float levelPower = 1.35f;

    [Header("Rewards")]
    public int scorePerTriple = 50;
    public int xpPerTriple = 20;

    public event Action<int> OnScoreChanged;                 // totalScore
    public event Action<int, int> OnXpChanged;                // xp, xpToNext
    public event Action<int> OnLevelUp;                      // new level

    const string K_LEVEL = "pp_level";
    const string K_XP = "pp_xp";
    const string K_SCORE = "pp_score";

    [Header("Currency")]
    [SerializeField] int coins = 0;

    public int Coins => coins;

    public event Action<int> OnCoinsChanged;

    const string K_COINS = "pp_coins";

    [Header("Level Rewards")]
    public int baseCoinsForLevel = 50;
    public int coinsPerLevel = 20;

    public int GetCoinsForLevelComplete()
    {
        return baseCoinsForLevel + (level * coinsPerLevel);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        RecalcXpToNext();
        ClampXp();
    }

    public int Level => level;
    public int XP => xp;
    public int XPToNext => xpToNext;
    public int TotalScore => totalScore;

    public void ResetProgress()
    {
        level = 1;
        xp = 0;
        totalScore = 0;
        RecalcXpToNext();
        Save();

        OnScoreChanged?.Invoke(totalScore);
        OnXpChanged?.Invoke(xp, xpToNext);
    }

    // ✅ вызывать, когда удалили тройки
    public void AddClear(int triplesCleared, int comboMultiplier)
    {
        if (triplesCleared <= 0) return;
        comboMultiplier = Mathf.Max(1, comboMultiplier);

        int gainedScore = triplesCleared * scorePerTriple * comboMultiplier;
        int gainedXp = triplesCleared * xpPerTriple * comboMultiplier;

        totalScore += gainedScore;
        xp += gainedXp;

        OnScoreChanged?.Invoke(totalScore);

        // level up loop
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

    void RecalcXpToNext()
    {
        // xpToNext = base * level^power
        xpToNext = Mathf.Max(10, Mathf.RoundToInt(baseXpToNext * Mathf.Pow(level, levelPower)));
    }

    void ClampXp()
    {
        if (xp < 0) xp = 0;
        if (level < 1) level = 1;
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
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        Save();
        OnCoinsChanged?.Invoke(coins);
    }

}
