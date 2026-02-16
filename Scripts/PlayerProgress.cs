using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[System.Serializable]
public class RoomItemState
{
    public string id;          // RoomItemId.ToString()
    public int equipped = 0;   // какой вариант установлен
    public int ownedMask = 1;  // битовая маска owned (бит0=дефолт)
    public int unlockedMask = 1; // битовая маска unlocked (бит0=дефолт)
    public int seenMask = 1;   // битовая маска seen (бит0 можно считать true)
}
[System.Serializable]
public class RoomProgressSaveData
{
    public List<RoomItemState> items = new List<RoomItemState>();
}
[Serializable]
public class NoAdsOfferSaveData
{
    public int levelsWon = 0;
    public int adsSeen = 0;
    public int continueUsed = 0;

    public int lastOfferDay = -1;     // yyyymmdd
    public long lastOfferUtcTicks = 0;
}
[Serializable]
public class CollectionSaveData
{
    public int currentIndex = 0;
    public int pendingTriples = 0;
    public float[] progress; // обязательно array для JsonUtility
}
[Serializable]
public class BoostSaveItem
{
    public BoostType type;
    public int count;
}
[Serializable]
public class PlayerProgressData
{
    public int level = 1;
    public int xp = 0;
    public int totalScore = 0;
    public int coins = 0;

    public int gameLevel = 1;    // ✅ УРОВЕНЬ ИГРЫ (run/level)

    public bool musicOn = true; // ✅
    public bool sfxOn = true;   // ✅

    public CollectionSaveData collection = new CollectionSaveData(); // ✅
    public NoAdsOfferSaveData noAdsOffer = new NoAdsOfferSaveData();
    public RoomProgressSaveData room = new RoomProgressSaveData(); // ✅
    public List<BoostSaveItem> boosts = new List<BoostSaveItem>(); // ✅ добавили
                                                                   // ✅ вот это добавь
    public List<string> shownHints = new List<string>();

    public bool noAds = false;
    public bool infiniteBoosters = false;
}
public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    [Header("Persistent (runtime)")]
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
    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnXpChanged;
    public event Action<int> OnLevelUp;
    public event Action<int> OnCoinsChanged;

    [SerializeField] int gameLevel = 1;

    public int GameLevel => gameLevel;
    public event Action<int> OnGameLevelChanged;

    public CollectionSaveData Collection { get; private set; } = new CollectionSaveData();
    public NoAdsOfferSaveData NoAdsOffer { get; private set; } = new NoAdsOfferSaveData();
    public RoomProgressSaveData Room { get; private set; } = new RoomProgressSaveData();
    public bool IsLoaded { get; private set; } = false;
    public event Action OnLoaded; // ✅ чтобы другие (AudioManager) узнали, что данные пришли
    public event Action OnInfiniteBoostersChanged;

    public bool MusicOn { get; private set; } = true;
    public bool SfxOn { get; private set; } = true;

    // ===== Local save key (Editor / non-WebGL) =====
    const string K_LOCAL = "pp_data_v1";
    // ✅ Храним показанные подсказки
    private HashSet<string> shownHints = new HashSet<string>();

    public bool NoAds { get; private set; }
    public bool InfiniteBoosters { get; private set; }

    Dictionary<BoostType, int> boosts = new Dictionary<BoostType, int>();
    public int GetBoost(BoostType t) => boosts.TryGetValue(t, out var v) ? v : 0;


    // ===== WebGL extern =====
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void SaveExtern(string data);
    [DllImport("__Internal")] private static extern void LoadExtern();
    [DllImport("__Internal")] private static extern void ResetCloudSave();
#endif

    // Чтобы не спамить сохранениями во время анимированных начислений/Load
    bool suppressAutoSave = false;

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

        // Важно: сначала загрузить, потом пересчитать
#if UNITY_WEBGL && !UNITY_EDITOR
        // Загрузка асинхронная: JS потом вызовет SetPlayerProgress(json)
        RequestCloudLoad();
#else
        LoadLocal();
        AfterLoaded();
#endif
    }

    // -------------------- LOAD/SAVE --------------------

    void RequestCloudLoad()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            LoadExtern();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerProgress] LoadExtern failed: {e.Message}");
            // если вдруг JS нет — хотя бы дефолт
            AfterLoaded();
        }
#endif
    }

    // ✅ ЭТОТ МЕТОД ВЫЗЫВАЕТ JS, когда пришли данные (как у тебя SetPlayerInfo)
    public void SetPlayerProgress(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            // если облако вернуло пусто — дефолт
            AfterLoaded();
            return;
        }

        PlayerProgressData data = null;
        try
        {
            data = JsonUtility.FromJson<PlayerProgressData>(json);
        }
        catch
        {
            data = null;
        }

        if (data == null)
        {
            AfterLoaded();
            return;
        }

        ApplyData(data);
        AfterLoaded();
    }

    void Save()
    {
        if (suppressAutoSave) return;

        var data = new PlayerProgressData
        {
            level = level,
            xp = xp,
            totalScore = totalScore,
            coins = coins,
            gameLevel = gameLevel, // ✅
            musicOn = MusicOn,   // ✅
            sfxOn = SfxOn,        // ✅
            collection = Collection, // ✅
            room = Room,
            noAdsOffer = NoAdsOffer,
            shownHints = shownHints.ToList(),
            noAds = NoAds,
            infiniteBoosters = InfiniteBoosters,

            // ✅ бустеры
            boosts = boosts.Select(kv => new BoostSaveItem { type = kv.Key, count = kv.Value }).ToList()
        };

        string json = JsonUtility.ToJson(data);
        Debug.Log("Игра сохранена");

#if UNITY_WEBGL && !UNITY_EDITOR
        SaveExtern(json);
#else
        PlayerPrefs.SetString(K_LOCAL, json);
        PlayerPrefs.Save();

#endif
    }

    void LoadLocal()
    {
        if (!PlayerPrefs.HasKey(K_LOCAL))
            return;

        var json = PlayerPrefs.GetString(K_LOCAL, "");
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            var data = JsonUtility.FromJson<PlayerProgressData>(json);
            if (data != null) ApplyData(data);
        }
        catch { }
    }

    void ApplyData(PlayerProgressData data)
    {
        level = data.level;
        xp = data.xp;
        totalScore = data.totalScore;
        coins = data.coins;

        MusicOn = data.musicOn; // ✅
        SfxOn = data.sfxOn;     // ✅

        Collection = data.collection ?? new CollectionSaveData(); // ✅
        gameLevel = Mathf.Max(1, data.gameLevel);
        NoAdsOffer = data.noAdsOffer ?? new NoAdsOfferSaveData();
        Room = data.room ?? new RoomProgressSaveData();
        shownHints = new HashSet<string>(data.shownHints ?? new List<string>());

        bool prevInfinite = InfiniteBoosters;
        NoAds = data.noAds;
        InfiniteBoosters = data.infiniteBoosters;

        if (prevInfinite != InfiniteBoosters)
            OnInfiniteBoostersChanged?.Invoke();
        boosts.Clear();
        if (data.boosts != null)
        {
            foreach (var b in data.boosts)
            {
                if (b == null) continue;
                boosts[b.type] = Mathf.Max(0, b.count);
            }
        }
    }

    void AfterLoaded()
    {
        RecalcXpToNext();
        ClampAll();

        // ✅ чтобы HUD/панели сразу обновились после загрузки
        OnScoreChanged?.Invoke(totalScore);
        OnCoinsChanged?.Invoke(coins);
        OnXpChanged?.Invoke(xp, xpToNext);
        OnLevelUp?.Invoke(level);
        IsLoaded = true;
        OnLoaded?.Invoke(); // ✅
        OnGameLevelChanged?.Invoke(gameLevel);
    }

    // -------------------- CALC REWARDS --------------------

    public int CalcCoinsForWin() => baseCoinsForLevel + level * coinsPerLevel;

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

    // -------------------- APPLY (INSTANT) --------------------

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        coins = Mathf.Max(0, coins + amount);
        OnCoinsChanged?.Invoke(coins);
        //Save();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        totalScore += amount;
        OnScoreChanged?.Invoke(totalScore);
        //Save();
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
        //Save();
    }

    // -------------------- APPLY (ANIMATED) --------------------
    // Важно: во время анимации не дёргаем Save в AddXp() каждую “микро-дельту”
    // Поэтому тут выключаем autosave и сохраняем один раз в конце.

    public void AddCoinsAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0) return;
        StartCoroutine(AddCoinsAnimatedRoutine(amount, duration));
    }

    public void AddScoreAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0) return;
        StartCoroutine(AddScoreAnimatedRoutine(amount, duration));
    }

    public void AddXpAnimated(int amount, float duration = 0.8f)
    {
        if (amount <= 0)
        {
            // если тебе прям надо форс-сохранение:
            Save();
            return;
        }
        StartCoroutine(AddXpAnimatedRoutine(amount, duration));
    }

    System.Collections.IEnumerator AddCoinsAnimatedRoutine(int amount, float duration)
    {
        suppressAutoSave = true;

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

        suppressAutoSave = false;
        Save();
    }

    System.Collections.IEnumerator AddScoreAnimatedRoutine(int amount, float duration)
    {
        suppressAutoSave = true;

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

        suppressAutoSave = false;
        //Save();
    }

    System.Collections.IEnumerator AddXpAnimatedRoutine(int amount, float duration)
    {
        suppressAutoSave = true;

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
                // ВАЖНО: AddXp дергает Save, но у нас suppressAutoSave=true
                AddXp(delta);
                lastApplied = curTotal;
            }

            yield return null;
        }

        int remain = targetTotal - lastApplied;
        if (remain > 0) AddXp(remain);

        suppressAutoSave = false;
        Save(); // один раз в конце
        SubmitTotalScoreToLeaderboard();
    }

    // -------------------- RESET / SPEND --------------------

    public void ResetProgress()
    {
        level = 1;
        xp = 0;
        totalScore = 0;
        coins = 0;

        RecalcXpToNext();
        ClampAll();
        //Save();

        OnScoreChanged?.Invoke(totalScore);
        OnCoinsChanged?.Invoke(coins);
        OnXpChanged?.Invoke(xp, xpToNext);
        OnLevelUp?.Invoke(level);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        AddCoins(-amount);
        return true;
    }

    // -------------------- INTERNAL --------------------

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
        if (xp > xpToNext) xp = Mathf.Clamp(xp, 0, xpToNext); // мягко
    }
    public void SetMusicOn(bool on)
    {
        MusicOn = on;
        //Save();
    }

    public void SetSfxOn(bool on)
    {
        SfxOn = on;
        //Save();
    }
    public void SaveCollection(CollectionSaveData data)
    {
        Collection = data ?? new CollectionSaveData();
        //Save();
    }
    public void SetGameLevel(int value)
    {
        int v = Mathf.Max(1, value);
        if (gameLevel == v) return;

        gameLevel = v;
        //Save();
        OnGameLevelChanged?.Invoke(gameLevel);
    }

    public void AdvanceGameLevel()
    {
        SetGameLevel(gameLevel + 1);
    }

    public void ResetRun()
    {
        SetGameLevel(1);
    }
    public void SaveNoAdsOffer(NoAdsOfferSaveData newData)
    {
        NoAdsOffer = newData ?? new NoAdsOfferSaveData();
        //Save();
    }
    public void SaveRoom(RoomProgressSaveData data)
    {
        Room = data ?? new RoomProgressSaveData();
        Save();
    }
    public bool HasShownHint(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return shownHints.Contains(id);
    }

    public void MarkHintShown(string id, bool saveNow = true)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (shownHints.Add(id) && saveNow)
            Save();
    }
    public void SubmitTotalScoreToLeaderboard()
    {
        const string LB = "Score"; // техническое имя из консоли

        if (!IsLoaded) return;
        if (LeaderboardBridge.Instance == null) return;

        LeaderboardBridge.Instance.SetScore(
            lbName: LB,
            score: totalScore,
            onOk: () => Debug.Log("✅ Leaderboard score sent: " + totalScore),
            onFail: err => Debug.LogWarning("⚠️ Leaderboard send failed: " + err)
        );
    }
    public void SetNoAds(bool value)
    {
        NoAds = value;
        Save();
    }
    public void SaveBoughtBoosts()
    {
        Save();
    }
    public void SaveRewards()
    {
        Save();
    }

    public void SetInfiniteBoosters(bool value)
    {
        if (InfiniteBoosters == value) return;
        InfiniteBoosters = value;
        //Save(); // если у тебя так

        OnInfiniteBoostersChanged?.Invoke();
    }
    public void ResetAllProgress()
    {
        Debug.Log("🧹 RESET ALL PROGRESS");

#if UNITY_WEBGL && !UNITY_EDITOR
    try
    {
        ResetCloudSave(); // вызов JS
    }
    catch (Exception e)
    {
        Debug.LogWarning("ResetCloudSave failed: " + e.Message);
    }
#endif

        // Локальный сброс
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Обнуляем текущие значения в памяти
        ResetProgress();
    }
    public void SetBoost(BoostType t, int value)
    {
        value = Mathf.Max(0, value);
        boosts[t] = value;
        Save(); // ✅ важно: чтобы улетало в облако
    }
}
