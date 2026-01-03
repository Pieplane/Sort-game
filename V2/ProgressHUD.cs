using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProgressHUD : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;
    public Slider xpSlider; // value 0..1

    void OnEnable()
    {
        StartCoroutine(BindWhenReady());
        PlayerProgress.Instance.OnCoinsChanged += UpdateCoins;
        UpdateCoins(PlayerProgress.Instance.Coins);
    }

    IEnumerator BindWhenReady()
    {
        // ждём пока появится Instance (важно если HUD включается раньше)
        while (PlayerProgress.Instance == null)
            yield return null;

        PlayerProgress.Instance.OnScoreChanged += UpdateScore;
        PlayerProgress.Instance.OnXpChanged += UpdateXp;
        PlayerProgress.Instance.OnLevelUp += UpdateLevel;

        UpdateScore(PlayerProgress.Instance.TotalScore);
        UpdateLevel(PlayerProgress.Instance.Level);
        UpdateXp(PlayerProgress.Instance.XP, PlayerProgress.Instance.XPToNext);
    }

    void OnDisable()
    {
        if (PlayerProgress.Instance == null) return;

        PlayerProgress.Instance.OnScoreChanged -= UpdateScore;
        PlayerProgress.Instance.OnXpChanged -= UpdateXp;
        PlayerProgress.Instance.OnLevelUp -= UpdateLevel;
        PlayerProgress.Instance.OnCoinsChanged -= UpdateCoins;
    }

    void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    void UpdateLevel(int lvl)
    {
        if (levelText) levelText.text = $"Lvl: {lvl}";
    }

    void UpdateXp(int xp, int toNext)
    {
        if (xpSlider) xpSlider.value = (toNext <= 0) ? 0f : (float)xp / toNext;
    }
    void UpdateCoins(int value)
    {
        coinsText.text = value.ToString();
    }
}
