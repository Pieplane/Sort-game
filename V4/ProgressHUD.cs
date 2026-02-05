using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProgressHUD : MonoBehaviour
{
    public static ProgressHUD Instance;

    TextMeshProUGUI scoreText;
    TextMeshProUGUI levelText;
    TextMeshProUGUI coinsText;
    Slider xpSlider;

    bool subscribed = false;
    Coroutine initRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // отписка от PlayerProgress
        if (subscribed && PlayerProgress.Instance != null)
        {
            PlayerProgress.Instance.OnScoreChanged -= UpdateScore;
            PlayerProgress.Instance.OnXpChanged -= UpdateXp;
            PlayerProgress.Instance.OnLevelUp -= UpdateLevel;
            PlayerProgress.Instance.OnCoinsChanged -= UpdateCoins;
        }
    }

    void Start()
    {
        // Init запускаем один раз
        if (initRoutine == null)
            initRoutine = StartCoroutine(InitOnce());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindUI();
        RefreshAll();
    }

    IEnumerator InitOnce()
    {
        while (PlayerProgress.Instance == null)
            yield return null;

        if (!subscribed)
        {
            PlayerProgress.Instance.OnScoreChanged += UpdateScore;
            PlayerProgress.Instance.OnXpChanged += UpdateXp;
            PlayerProgress.Instance.OnLevelUp += UpdateLevel;
            PlayerProgress.Instance.OnCoinsChanged += UpdateCoins;
            subscribed = true;
        }

        RebindUI();
        RefreshAll();
    }

    void RefreshAll()
    {
        if (PlayerProgress.Instance == null) return;

        UpdateScore(PlayerProgress.Instance.TotalScore);
        UpdateLevel(PlayerProgress.Instance.Level);
        UpdateXp(PlayerProgress.Instance.XP, PlayerProgress.Instance.XPToNext);
        UpdateCoins(PlayerProgress.Instance.Coins);
    }

    // ========================= BIND UI =========================

    void RebindUI()
    {
        var hudRoot = GameObject.Find("HUDRoot");
        if (hudRoot != null)
        {
            coinsText = hudRoot.transform.Find("Money")
                ?.GetComponentInChildren<TextMeshProUGUI>(true);

            scoreText = hudRoot.transform.Find("Points")
                ?.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            coinsText = null;
            scoreText = null;
        }

        var levelGO = GameObject.Find("Level");
        levelText = levelGO != null
            ? levelGO.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;

        var sliderGO = GameObject.Find("Slider");
        xpSlider = sliderGO != null
            ? sliderGO.GetComponent<Slider>()
            : null;
    }

    // ========================= UPDATE UI =========================

    void UpdateScore(int value)
    {
        if (scoreText) scoreText.text = value.ToString();
    }

    void UpdateLevel(int lvl)
    {
        if (levelText) levelText.text = $"Ранг {lvl}";
    }

    void UpdateXp(int xp, int toNext)
    {
        //Debug.Log($"HUD XP: {xp}/{toNext}");
        if (xpSlider)
            xpSlider.value = (toNext <= 0) ? 0f : (float)xp / toNext;
    }

    void UpdateCoins(int value)
    {
        if (coinsText) coinsText.text = value.ToString();
    }
}
