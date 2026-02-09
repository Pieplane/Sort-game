using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Level")]
    public LevelConfig level;

    [Header("UI")]
    public Canvas canvas;

    [Header("Prefabs")]
    public DraggableItem itemPrefab;

    [Header("Visuals")]
    //public ItemVisualConfig visuals;
    [SerializeField] private ItemVisualConfig defaultVisuals;

    [Header("Debug")]
    public bool debugLog = true;

    private readonly List<ShelfView> shelves = new List<ShelfView>();

    // ---- Back system (GLOBAL) ----
    private readonly Queue<ItemType> globalBackBag = new Queue<ItemType>();
    private readonly Dictionary<ShelfView, int> shelfPacksLeft = new Dictionary<ShelfView, int>();

    bool levelCompleted = false;
    int levelClearedTriplesTotal = 0; // сколько троек убрал игрок за весь уровень

    [SerializeField] private WinPanel winPanel;
    [SerializeField] private LosePanelUI losePanel;

    // ===== PENDING REWARDS (не начислены) =====
    int pendingCoins;
    int pendingScore;
    int pendingXp;
    bool rewardClaimed;

    Coroutine rewardRoutine;

    bool multiplierUsed = false;
    int chosenMultiplier = 1;

    Coroutine claimRoutine;
    [SerializeField] private MultiplierMeter multiplierMeter;

    [SerializeField] private CollectionManager collectionManager;

    [SerializeField] private NoAdsOfferManager noAdsOffer;

    [SerializeField] private string menuSceneName = "Menu"; // имя сцены меню
    [SerializeField] private LevelConfig[] levels;

    // ===== ITEM REGISTRY (no FindObjectsOfType) =====
    private readonly List<ItemView> allItems = new List<ItemView>(256);
    private int activeClearingItems = 0;

    [Header("Tutorial Hint (Level 1)")]
    [SerializeField] private bool tutorialHintLevel1 = true;
    [SerializeField] private RectTransform hintFinger; // UI картинка пальца на Canvas
    private bool hintShownThisRun;

    [SerializeField] private Vector2 hintToOffset = new Vector2(0f, -17f); // вниз

    [Header("Loop visuals/layout after max")]
    [SerializeField] private bool loopVisuals = true;
    [SerializeField] private int loopFromLevel = 18; // включительно
    [SerializeField] private int loopToLevel = 25;   // включительно

    public int ItemsCount => allItems.Count;
    bool AnyItemClearing() => activeClearingItems > 0;
    int CountAllItems() => allItems.Count;

    private ItemVisualConfig CurrentVisuals => (level != null && level.visuals != null) ? level.visuals : defaultVisuals;

    [SerializeField] private Transform[] layoutRoots; // сюда закинешь Layout_2Shelves, Layout_3Shelves...
    private Transform activeLayoutRoot;

    ItemVisualConfig currentVisualsOverride = null;
    int? currentLayoutIndexOverride = null;

    //private bool playerMadeMove = false;

    [SerializeField] private MusicDirector musicDirector;

    public bool ModalOpen { get; private set; }

    [Header("Reward Popup")]
    [SerializeField] private RewardPanel rewardPanel; // твоя новая панель
    //[SerializeField] private int triplesToReward = 5;
    private int triplesSinceReward = 0;
    private bool rewardQueued = false;

    private bool lastMoveHadTriples;

    // queued reward values (фиксируем при постановке в очередь)
    int queuedRewardCoins;
    //int queuedRewardXp;
    //int queuedRewardScore;


    struct Pack
    {
        public ItemType a, b, c;
        public Pack(ItemType a, ItemType b, ItemType c) { this.a = a; this.b = b; this.c = c; }
    }

    private readonly Dictionary<ShelfView, Pack> reservedPack = new Dictionary<ShelfView, Pack>();

    void Awake()
    {
        Instance = this;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        DOTween.SetTweensCapacity(2000, 400);
    }
    void OnDisable()
    {
        DOTween.Kill(gameObject);   // убьёт все твины, залинкованные на этот GO
    }

    void Start()
    {
        // прогрев DOTween, чтобы первый реальный tween не лагал
        DOTween.To(() => 0f, x => { }, 1f, 0.01f).SetUpdate(true).Kill();

        // прогрев UI rebuild (часто помогает, если есть layout группы)
        Canvas.ForceUpdateCanvases();

        SetupLevelForGameLevel(GameFlow.GameLevel);

        if (musicDirector != null)
        {
            musicDirector.ApplyForLevel(GameFlow.GameLevel);
        }

        ApplyLayoutFromLevel();

        if (shelves.Count == 0)
        {
            Debug.LogError("BoardManager: Не найдено ни одной ShelfView на сцене.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("BoardManager: Не назначен itemPrefab.");
            return;
        }

        if (CurrentVisuals == null)
        {
            Debug.LogError("BoardManager: Не назначен ItemVisualConfig (visuals).");
            return;
        }
        if (collectionManager != null)
        {
            collectionManager.IsBoardIdle = () =>
                !AnyShelfClearing() && !AnyItemClearing();

            collectionManager.TryOpenModal = TryOpenModal;
            collectionManager.CloseModal = CloseModal;
        }
        if (losePanel != null)
        {
            losePanel.OnRestart = () =>
            {
                if (AudioManager.Instance != null)
                {
                    // ================================== 🎧 AUDIO MANAGER CALL ==================================
                    AudioManager.Instance.Play("Click");
                }
                losePanel.Hide();
                RestartLevel();
            };

            losePanel.OnContinue = ContinueAfterLose;
        }

        
        SpawnLevel();
        InitBackSystem();

        StartCoroutine(InitialRefillWithFailsafe());
        StartCoroutine(TutorialHint_Level1());
    }

    // Эти методы нужны DraggableItem (оставляем)
    public void RegisterDragging(DraggableItem item) { }
    public void UnregisterDragging(DraggableItem item) { }

    // Вызывается из SlotView после успешного дропа
    public void OnItemPlaced(SlotView slot)
    {
        HideTutorialFinger();
        StartCoroutine(AfterMoveRoutine());
    }

    IEnumerator AfterMoveRoutine()
    {
        if (levelCompleted) yield break;

        int triples = ResolveAllTriples_Count();
        lastMoveHadTriples = triples > 0;

        if (triples > 0)
            levelClearedTriplesTotal += triples;



        yield return StartCoroutine(AfterBoardChangedRoutine());
    }

    // ===================== LEVEL SPAWN =====================

    public void SpawnLevel()
    {
        int totalSlots = GetTotalSlots();
        int emptyTotal = GetEmptyTotal(totalSlots);

        const int maxAttempts = 500;
        List<ItemTypeOrEmpty> best = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var layout = BuildLayoutMultipleOf3(totalSlots, emptyTotal);
            if (!HasTripleInLayout(layout))
            {
                best = layout;
                break;
            }
        }

        if (best == null)
        {
            Debug.LogWarning("SpawnLevel: не смог подобрать старт без троек за maxAttempts, оставляю как есть.");
            best = BuildLayoutMultipleOf3(totalSlots, emptyTotal);
        }

        ClearAllSlots();
        ApplyLayout(best);
        levelCompleted = false;
        levelClearedTriplesTotal = 0;
    }

    // ===================== UNIVERSAL POST =====================

    IEnumerator AfterBoardChangedRoutine()
    {
        yield return new WaitUntil(() => !AnyShelfClearing() && !AnyItemClearing());
        //yield return new WaitForSecondsRealtime(0.08f);


        yield return null; // Destroy flush

        RefillEmptyShelvesNow();

        for (int guard = 0; guard < 10; guard++)
        {
            int triples = ResolveAllTriples_Count();
            if (triples <= 0) break;
            yield return new WaitUntil(() => !AnyShelfClearing() && !AnyItemClearing());
            yield return null; RefillEmptyShelvesNow();
        }

        

        if (CountAllItems() == 0)
        {
            HandleLevelComplete();
            yield break;
        }

        if (!HasAnyEmptySlot())
        {
            GameOver();
            yield break;
        }
        if (lastMoveHadTriples)
        {
            if (collectionManager != null && !ModalOpen)
                collectionManager.OnBoardBecameIdle();

            // ✅ ВОТ ЭТОГО НЕ ХВАТАЛО
            if (!ModalOpen)
                TryShowRewardPopup();
        }
        lastMoveHadTriples = false;
    }

    bool AnyShelfClearing()
    {
        foreach (var sh in shelves)
            if (sh.ActiveClears > 0) return true;
        return false;
    }

    // ===================== BACK SYSTEM =====================

    void InitBackSystem()
    {
        shelfPacksLeft.Clear();
        globalBackBag.Clear();
        reservedPack.Clear();

        var types = (level != null && level.allowedTypes != null && level.allowedTypes.Length > 0)
            ? level.allowedTypes
            : new[] { ItemType.A, ItemType.B };

        int totalPacks = 0;
        foreach (var sh in shelves)
        {
            if (!sh.hasBackItems) continue;

            int packs = Mathf.Max(0, sh.backPacks);
            shelfPacksLeft[sh] = packs;
            totalPacks += packs;
        }

        if (totalPacks == 0)
        {
            foreach (var sh in shelves) sh.HideBackPreview();
            return;
        }
        BuildBalancedBackBag_AsPacks(types, totalPacks);

        // показать превью на старте
        foreach (var sh in shelves)
        {
            if (!sh.hasBackItems) { sh.HideBackPreview(); continue; }

            if (shelfPacksLeft.TryGetValue(sh, out int left) && left > 0)
                ReserveNextPackForShelf(sh);
            else
                sh.HideBackPreview();
        }
    }

    void RefillEmptyShelvesNow()
    {
        foreach (var sh in shelves)
        {
            if (!sh.hasBackItems) continue;
            if (!sh.IsEmpty3()) continue;

            // ✅ важное: если это последние пустые слоты — НЕ заполняем
            // иначе игрок сразу без хода
            //if (CountEmptySlots() <= 3)
            //{
            //    if (debugLog) Debug.Log("Refill skipped: would remove last empty slots");
            //    continue;
            //}

            if (!shelfPacksLeft.TryGetValue(sh, out int left) || left <= 0)
            {
                sh.HideBackPreview();
                continue;
            }

            if (!reservedPack.ContainsKey(sh))
                ReserveNextPackForShelf(sh);

            if (!reservedPack.TryGetValue(sh, out var p))
                continue;

            sh.PlacePack(this, CurrentVisuals, p.a, p.b, p.c);

            reservedPack.Remove(sh);
            shelfPacksLeft[sh] = left - 1;

            // резервируем следующую пачку и обновляем превью
            ReserveNextPackForShelf(sh);
        }
    }

    void ReserveNextPackForShelf(ShelfView sh)
    {
        if (reservedPack.ContainsKey(sh)) return;

        if (!shelfPacksLeft.TryGetValue(sh, out int left) || left <= 0)
        {
            sh.HideBackPreview();
            return;
        }

        if (!TryDrawPack(out var a, out var b, out var c))
        {
            sh.HideBackPreview();
            return;
        }

        var p = new Pack(a, b, c);
        reservedPack[sh] = p;

        // затемнённое превью
        sh.ShowBackPreview(CurrentVisuals, a, b, c);
    }

    // ===================== TRIPLES =====================

    int ResolveAllTriples_Count()
    {
        int triplesTotal = 0;

        for (int guard = 0; guard < 20; guard++)
        {
            bool clearedThisPass = false;

            foreach (var sh in shelves)
            {
                if (sh.TryClearTriple())
                {
                    clearedThisPass = true;
                    triplesTotal++;

                    // ✅ ВАЖНО: регистрируем тройку для коллекции
                    if (collectionManager != null)
                        collectionManager.OnTripleMatched();
                }
            }

            if (!clearedThisPass)
                break;
        }

        if (triplesTotal > 0 && debugLog)
            Debug.Log($"✅ Тройки удалены: {triplesTotal}");

        // твой rewardQueued — оставь как есть
        if (triplesTotal > 0)
        {
            triplesSinceReward += triplesTotal;

            int need = GetTriplesToRewardNow();
            //Debug.Log($"REWARD COUNTER: +{triplesTotal} => {triplesSinceReward}/{triplesToReward}");
            if (triplesSinceReward >= need)
            {
                rewardQueued = true;
                triplesSinceReward = 0;
                //Debug.Log("REWARD QUEUED ✅");

                queuedRewardCoins = GetRewardCoinsNow();
                //queuedRewardXp = 0;
                //queuedRewardScore = 0;

                //Debug.Log($"REWARD QUEUED ✅ coins={queuedRewardCoins}");
            }
        }

        return triplesTotal;
    }

    // ===================== CORE HELPERS =====================

    int GetTotalSlots()
    {
        int total = 0;
        foreach (var sh in shelves) total += sh.slots.Count;
        return total;
    }

    int GetEmptyTotal(int totalSlots)
    {
        int empty = 1;
        if (level != null)
            empty = Mathf.Clamp(level.emptySlotsTotal, 1, totalSlots - 1);
        return empty;
    }

    void ClearAllSlots()
    {
        foreach (var sh in shelves)
        {
            foreach (var slot in sh.slots)
            {
                var iv = slot.GetItemView();
                if (iv != null) Destroy(iv.gameObject);
            }
        }
    }

    void ApplyLayout(List<ItemTypeOrEmpty> layout)
    {
        int index = 0;

        for (int s = 0; s < shelves.Count; s++)
        {
            var shelf = shelves[s];

            for (int k = 0; k < shelf.slots.Count; k++)
            {
                if (index >= layout.Count) return;

                var slot = shelf.slots[k];
                var cell = layout[index++];

                if (cell == ItemTypeOrEmpty.Empty) continue;

                var item = Instantiate(itemPrefab, slot.contentRoot);
                item.name = "Item";

                var iv = item.GetComponent<ItemView>();
                if (iv == null) iv = item.gameObject.AddComponent<ItemView>();

                iv.SetType((ItemType)cell, CurrentVisuals);
                item.SetInSlot(slot);

                RegisterItem(iv);
            }
        }
    }

    bool HasTripleInLayout(List<ItemTypeOrEmpty> layout)
    {
        int index = 0;

        for (int s = 0; s < shelves.Count; s++)
        {
            var shelf = shelves[s];
            if (shelf.slots == null || shelf.slots.Count < 3)
            {
                index += (shelf.slots != null ? shelf.slots.Count : 0);
                continue;
            }

            var c0 = layout[index + 0];
            var c1 = layout[index + 1];
            var c2 = layout[index + 2];

            if (c0 != ItemTypeOrEmpty.Empty && c0 == c1 && c1 == c2)
                return true;

            index += shelf.slots.Count;
        }

        return false;
    }

    bool HasAnyEmptySlot()
    {
        foreach (var sh in shelves)
            foreach (var slot in sh.slots)
                if (slot.GetItemView() == null)
                    return true;
        return false;
    }

    void GameOver()
    {
        if (levelCompleted) return; // если вдруг победа — не показываем проигрыш
        levelCompleted = true;      // блокируем дальнейшие ходы/корутины (если тебе так надо)

        Debug.Log("💀 GAME OVER: нет свободных слотов");

        if (losePanel != null)
            losePanel.Show();
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Lose");
        }

    }

    public void RestartLevel()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        // 1) очистка
        ClearAllSlots();

        // 2) ждём, пока реальные draggable-предметы уйдут из слотов (до 5 кадров)
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            if (CountAllDraggablesInSlots() == 0) break;
        }

        // 3) спавн старта
        SpawnLevel();

        // 4) допы
        InitBackSystem();

        // 5) кадр чтобы ApplyLayout точно встал
        yield return null;

        // 6) если полка пустая и есть допы — выдвигаем
        RefillEmptyShelvesNow();
    }

    int CountAllDraggablesInSlots()
    {
        int count = 0;
        foreach (var sh in shelves)
        {
            foreach (var sl in sh.slots)
            {
                if (sl == null || sl.contentRoot == null) continue;

                for (int i = 0; i < sl.contentRoot.childCount; i++)
                {
                    if (sl.contentRoot.GetChild(i).GetComponent<DraggableItem>() != null)
                        count++;
                }
            }
        }
        return count;
    }

    // ===================== BUFF #1 (SAFE) =====================

    /// <summary>
    /// Удаляет ТОЛЬКО кратно 3 одного типа (1 => 3 предмета, 2 => 6 предметов...)
    /// чтобы не ломать решаемость.
    /// </summary>
    public void Buff_RemoveTripletsOfMostCommonType(int tripletsToRemove)
    {
        int need = Mathf.Max(1, tripletsToRemove) * 3;

        var snapshot = new List<ItemView>(allItems);
        snapshot.RemoveAll(v => v == null || v.IsClearing);
        if (snapshot.Count == 0) return;

        var groups = new Dictionary<ItemType, List<ItemView>>();

        foreach (var it in snapshot)
        {
            if (it == null || it.IsClearing) continue;

            if (!groups.TryGetValue(it.Type, out var list))
            {
                list = new List<ItemView>();
                groups[it.Type] = list;
            }
            list.Add(it);
        }

        ItemType chosen = default;
        int bestCount = 0;

        foreach (var kv in groups)
        {
            int c = kv.Value.Count;
            if (c >= 3 && c > bestCount)
            {
                bestCount = c;
                chosen = kv.Key;
            }
        }

        if (bestCount < 3)
        {
            Debug.Log("🧨 Buff: нет типа с >= 3 предметами. Бафф не сработал.");
            return;
        }

        int canRemove = (bestCount / 3) * 3;
        int removeCount = Mathf.Min(need, canRemove);

        var listChosen = groups[chosen];

        for (int i = 0; i < removeCount; i++)
        {
            var item = listChosen[i];
            if (item == null || item.IsClearing) continue;

            // ✅ звёзды как при обычной тройке
            var shelf = item.GetComponentInParent<ShelfView>();
            if (shelf != null)
                shelf.PlayStarsForItem(item);

            item.MarkClearing();
            StartCoroutine(item.PlayClearPopCollapse());
        }
        // ✅ СКОЛЬКО ТРОЕК УДАЛИЛ БАФФ
        int removedTriplets = removeCount / 3;
        if (removedTriplets > 0)
        {
            // 1) для победных наград (если хочешь учитывать бафф в скоре/награде)
            levelClearedTriplesTotal += removedTriplets;

            // 2) счетчик ревард-панели
            triplesSinceReward += removedTriplets;
            //Debug.Log($"REWARD COUNTER (BUFF): +{removedTriplets} => {triplesSinceReward}/{triplesToReward}");

            int need2 = GetTriplesToRewardNow();

            if (triplesSinceReward >= need2)
            {
                rewardQueued = true;
                triplesSinceReward = 0;
                Debug.Log("REWARD QUEUED ✅ (BUFF)");
            }

            // 3) счетчик коллекции (важно дернуть N раз, чтобы pendingTriples вырос корректно)
            if (collectionManager != null)
            {
                for (int t = 0; t < removedTriplets; t++)
                    collectionManager.OnTripleMatched();
            }

            // 4) чтобы AfterBoardChangedRoutine показал попапы
            lastMoveHadTriples = true;
        }

        StartCoroutine(AfterBoardChangedRoutine());
    }

    // ===================== LAYOUT GENERATION (AS IS) =====================

    List<ItemTypeOrEmpty> BuildLayoutMultipleOf3(int totalSlots, int emptyTotal)
    {
        int itemsCount = totalSlots - emptyTotal;

        while (itemsCount % 3 != 0 && emptyTotal < totalSlots - 1)
        {
            emptyTotal++;
            itemsCount = totalSlots - emptyTotal;
        }

        if (debugLog) Debug.Log("itemsCount " + itemsCount);

        var layout = new List<ItemTypeOrEmpty>(totalSlots);

        int triples = itemsCount / 3;

        var types = (level != null && level.allowedTypes != null && level.allowedTypes.Length > 0)
            ? level.allowedTypes
            : new[] { ItemType.A, ItemType.B };

        for (int t = 0; t < triples; t++)
        {
            var type = types[Random.Range(0, types.Length)];
            layout.Add((ItemTypeOrEmpty)type);
            layout.Add((ItemTypeOrEmpty)type);
            layout.Add((ItemTypeOrEmpty)type);
        }

        for (int i = 0; i < emptyTotal; i++)
            layout.Add(ItemTypeOrEmpty.Empty);

        for (int i = 0; i < layout.Count; i++)
        {
            int j = Random.Range(i, layout.Count);
            (layout[i], layout[j]) = (layout[j], layout[i]);
        }

        return layout;
    }
    public void ClearSomeOfSameType(int count)
    {
        // старый метод -> новый безопасный
        Buff_RemoveTripletsOfMostCommonType(
            Mathf.Max(1, count / 3)
        );
    }

    public void ShuffleBoard()
    {
        ShuffleBoard_Internal();
    }
    void ShuffleBoard_Internal()
    {
        var items = new List<ItemView>(allItems);
        items.RemoveAll(i => i == null || i.IsClearing);
        if (items.Count == 0) return;

        List<SlotView> slots = new List<SlotView>();

        foreach (var sh in shelves)
            foreach (var sl in sh.slots)
                if (sl.GetItemView() != null)
                    slots.Add(sl);

        if (items.Count != slots.Count) return;

        for (int i = 0; i < slots.Count; i++)
        {
            int j = Random.Range(i, slots.Count);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        for (int i = 0; i < items.Count; i++)
        {
            var d = items[i].GetComponent<DraggableItem>();
            if (d == null) continue;

            d.SetParentToSlot(slots[i].contentRoot);
            d.SetInSlot(slots[i]);
            d.PlayPlaceBounce();
        }

        StartCoroutine(AfterBoardChangedRoutine());
    }

    IEnumerator InitialRefillIfNeeded()
    {
        // 1 кадр, чтобы инстансы стартовых предметов точно появились в иерархии
        yield return null;

        // ВАЖНО: только выдвинуть допы, без ResolveAllTriples()
        RefillEmptyShelvesNow();
    }
    void HandleLevelComplete()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        // ✅ Победа = модалка. Просто ставим флаг, без TryOpenModal()
        ModalOpen = true;

        rewardClaimed = false;

        int stars = 3;

        pendingCoins = PlayerProgress.Instance.CalcCoinsForWin();
        pendingScore = PlayerProgress.Instance.CalcScoreForWin(levelClearedTriplesTotal, 1, multiplier: 1);
        pendingXp = PlayerProgress.Instance.CalcXpForWin(stars, multiplier: 1);

        winPanel.Show(stars, pendingCoins, pendingScore);
        multiplierMeter.gameObject.SetActive(true);
        multiplierMeter.ResetAndStart();

        FindFirstObjectByType<ConfettiFX>()?.Play();

        // ВАЖНО: не запускаем RewardSequence тут
    }
    public void OnClick_Multiplier()
    {
        if (!levelCompleted) return;
        if (multiplierUsed) return;

        chosenMultiplier = multiplierMeter != null ? multiplierMeter.StopAndGetMultiplier() : 1;
        chosenMultiplier = Mathf.Max(1, chosenMultiplier);
        multiplierUsed = true;

        // Обнови числа на панели (визуально)
        int coinsFinal = pendingCoins * chosenMultiplier;
        int scoreFinal = pendingScore * chosenMultiplier;

        winPanel.SetRewardTexts(coinsFinal, scoreFinal, chosenMultiplier);

        ClaimRewards(multiplier: chosenMultiplier, 1f);
    }
    public void OnClick_Next()
    {
        ClaimRewards(multiplier: 1, 0f);
    }
    void ClaimRewards(int multiplier, float delay)
    {
        if (rewardClaimed) return;
        rewardClaimed = true;

        int coins = pendingCoins * multiplier;
        int score = pendingScore * multiplier;
        int xp = pendingXp * multiplier;
        //int xp = pendingXp;

        // ✅ ВАЖНО: повышаем уровень сразу
        GameFlow.AdvanceGameLevel();
        Debug.Log($"ADVANCE -> GameLevel={GameFlow.GameLevel}");

        // ✅ регистрируем оффер
        noAdsOffer?.RegisterLevelWon();

        if (GameFlow.IsOnboarding)
        {
            // ===== УРОВНИ 1-3: начисляем тут и идём на следующий уровень в этой же сцене =====
            StartCoroutine(RewardSequence(coins, score, xp, delay));

            LoadLevelByGameLevel(GameFlow.GameLevel);
        }
        else
        {
            // ===== УРОВНИ 4+: переносим награды в меню =====
            PendingRewards.Set(new PendingRewardsData
            {
                coins = coins,
                score = score,
                xp = xp,
                multiplier = multiplier,
                stars = 3,
                sourceLevel = GameFlow.GameLevel
            });

            //noAdsOffer?.TryShowOffer("after_win");
            noAdsOffer?.RequestOfferNextScene("win->menu");
            StartCoroutine(TransitionToMenuAfterDelay(delay));
        }
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Success2");
        }
        //Debug.Log($"CLAIM: GameLevel={GameFlow.GameLevel}, IsOnboarding={GameFlow.IsOnboarding}");
    }
    IEnumerator TransitionToMenuAfterDelay(float delay)
    {
        // ⏸ даём доиграть эффектам (звёзды / pop / множитель)
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // 🔒 важно: сначала аккуратно закрыть панель
        if (winPanel != null)
            winPanel.Hide(); // твины убиваются внутри

        CloseModal(); // ✅ отпустить модалку
        // ⏸ один кадр — чтобы DOTween успел всё убить
        yield return null;

        // 🚪 переход в меню
        SceneManager.LoadScene(menuSceneName);
    }
    void LoadLevelByGameLevel(int gameLevel)
    {
        SetupLevelForGameLevel(gameLevel);
        if(musicDirector!= null)
        {
            musicDirector.ApplyForLevel(gameLevel);
        }
        triplesSinceReward = 0;
        rewardQueued = false;

        if (losePanel != null) losePanel.Hide();

        ApplyLayoutFromLevel(); // он должен использовать currentLayoutIndexOverride
        RestartLevel();
    }

    IEnumerator RewardSequence(int coinsReward, int scoreReward, int xpReward, float delayReward)
    {
        yield return new WaitForSecondsRealtime(delayReward);
        if (winPanel != null) winPanel.Hide();
        CloseModal(); // ✅
        multiplierMeter.gameObject.SetActive(false);
        multiplierUsed = false;
        // ===== COINS FX =====
        var coinSpawn = GameObject.Find("WinCoinSpawn")?.GetComponent<RectTransform>();
        if (coinSpawn != null && CoinFlyDOTween.Instance != null)
            CoinFlyDOTween.Instance.Play(coinSpawn.anchoredPosition);

        yield return new WaitForSecondsRealtime(0.6f);
        PlayerProgress.Instance.AddCoinsAnimated(coinsReward, 0.8f);

        yield return new WaitForSecondsRealtime(0.7f);

        // ===== SCORE FX =====
        var scoreSpawn = GameObject.Find("WinScoreSpawn")?.GetComponent<RectTransform>();
        if (scoreSpawn != null && CoinFlyDOTween.Instance != null) // если сделаешь как монеты
            CoinFlyDOTween.Instance.Play(scoreSpawn.anchoredPosition);

        yield return new WaitForSecondsRealtime(0f);

        // ===== XP FX (опционально) =====
        var xpSpawn = GameObject.Find("WinXPSpawn")?.GetComponent<RectTransform>();
        if (xpSpawn != null && XpFlyDOTween.Instance != null)
            XpFlyDOTween.Instance.Play(xpSpawn.anchoredPosition, startDelay: 0f);


        yield return new WaitForSecondsRealtime(0.55f);
        PlayerProgress.Instance.AddScoreAnimated(scoreReward, 0.9f);

        yield return new WaitForSecondsRealtime(0.55f);
        PlayerProgress.Instance.AddXpAnimated(xpReward, 0.9f); // если есть

        // закрываем панель
        if (winPanel != null) winPanel.Hide();
        multiplierMeter.gameObject.SetActive(false);

        
    }
    void ContinueAfterLose()
    {
        // закрываем панель
        losePanel.Hide();

        // разблокируем игру
        levelCompleted = false;

        // 🧨 ВАЖНО: сначала освобождаем место
        BuffManager.Instance?.Buff_ClearSomeOfType();

        noAdsOffer?.RegisterContinueUsed();
        noAdsOffer?.TryShowOffer("after_continue");

        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Collect");
            return;
        }
        // после баффа борд сам по себе:
        // - проиграет анимации
        // - вызовет AfterBoardChangedRoutine
        // - продолжит игру
    }
    IEnumerator EnsurePlayableAtStart_Co()
    {
        if (HasAnyEmptySlot()) yield break;

        Debug.Log("⚠️ FailSafe: нет свободных слотов на старте, удаляем тройку");
        Buff_RemoveTripletsOfMostCommonType(1);

        // подождать пока реально удалится и слот освободится
        yield return new WaitUntil(() => HasAnyEmptySlot());
        Debug.Log("✅ FailSafe: слот освободился");
    }
    IEnumerator InitialRefillWithFailsafe()
    {
        yield return StartCoroutine(InitialRefillIfNeeded());

        // один кадр, чтобы всё точно встало
        yield return null;

        //EnsurePlayableAtStart();
        yield return StartCoroutine(EnsurePlayableAtStart_Co());
    }
    public bool Buff_Hint_ThreeOrTwo(float seconds = 1.2f)
    {
        //var all = new List<ItemView>(FindObjectsOfType<ItemView>());
        var snapshot = new List<ItemView>(allItems);
        snapshot.RemoveAll(v => v == null || v.IsClearing);

        // группируем по типу
        var byType = new Dictionary<ItemType, List<ItemView>>();
        foreach (var v in snapshot)
        {
            //if (v == null || v.IsClearing) continue;

            if (!byType.TryGetValue(v.Type, out var list))
            {
                list = new List<ItemView>();
                byType[v.Type] = list;
            }
            list.Add(v);
        }

        // 1) ищем 3 одинаковых
        foreach (var kv in byType)
        {
            if (kv.Value.Count >= 3)
            {
                HighlightItem(kv.Value[0], seconds);
                HighlightItem(kv.Value[1], seconds);
                HighlightItem(kv.Value[2], seconds);
                return true;
            }
        }

        // 2) если 3 нет — ищем 2 одинаковых
        foreach (var kv in byType)
        {
            if (kv.Value.Count >= 2)
            {
                HighlightItem(kv.Value[0], seconds);
                HighlightItem(kv.Value[1], seconds);
                return true;
            }
        }

        Debug.Log("💡 Hint: нет даже пары одинаковых на поле.");
        return false;
    }

    void HighlightItem(ItemView item, float seconds)
    {
        if (item == null) return;

        var rt = item.GetComponent<RectTransform>();
        if (rt == null) rt = item.GetComponentInChildren<RectTransform>(true);
        if (rt == null) return;

        rt.DOKill();

        // Подсветка: большая пульсация
        rt.DOPunchScale(Vector3.one * 0.25f, 0.35f, 8, 0.8f);
        rt.DOScale(1.18f, 0.25f).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine);

        DOVirtual.DelayedCall(seconds, () =>
        {
            if (rt == null) return;
            rt.DOKill();
            rt.localScale = Vector3.one;
        });
    }
    // вызывать когда предмет появился
    public void RegisterItem(ItemView v)
    {
        if (v == null) return;
        if (!allItems.Contains(v))
            allItems.Add(v);
    }
    // вызывать перед уничтожением/отключением
    public void UnregisterItem(ItemView v)
    {
        if (v == null) return;
        allItems.Remove(v);
    }
    // когда предмет начал анимацию очистки
    public void NotifyItemClearingStart(ItemView v) 
    { 
        activeClearingItems++; 
    } 
    // когда предмет закончил очистку (перед Destroy / OnDisable)
    public void NotifyItemClearingEnd(ItemView v) 
    { 
        activeClearingItems = Mathf.Max(0, activeClearingItems - 1); 
    }
    IEnumerator TutorialHint_Level1()
    {
        if (!tutorialHintLevel1)
        {
            Debug.Log("TUTORIAL: disabled");
            yield break;
        }

        if (GameFlow.GameLevel != 1)
        {
            //Debug.Log("TUTORIAL: not level 1");
            yield break;
        }

        Debug.Log("TUTORIAL: waiting board idle...");
        yield return new WaitUntil(() => !AnyShelfClearing() && !AnyItemClearing());
        yield return null;

        //Debug.Log($"TUTORIAL: board idle ✅  items={allItems.Count} emptySlots={CountEmptySlots()}");

        bool found = TryFindMoveThatMakesTriple(out var item, out var emptySlot);
        Debug.Log($"TUTORIAL: TryFindMoveThatMakesTriple = {found}");

        if (!found)
        {
            Debug.LogWarning("TUTORIAL: no 1-move triple found on this board");
            yield break;
        }

        Debug.Log($"TUTORIAL: SHOW finger from {item.name} to slot {emptySlot.name}");
        ShowFinger(item, emptySlot);
    }
    bool TryFindMoveThatMakesTriple(out DraggableItem fromItem, out SlotView targetSlot)
    {
        fromItem = null;
        targetSlot = null;

        // пустые слоты
        var emptySlots = new List<SlotView>(16);
        foreach (var sh in shelves)
            foreach (var sl in sh.slots)
                if (sl.GetItemView() == null)
                    emptySlots.Add(sl);

        if (emptySlots.Count == 0) return false;

        // предметы
        var items = new List<DraggableItem>(allItems.Count);
        foreach (var iv in allItems)
        {
            if (iv == null || iv.IsClearing) continue;
            var d = iv.GetComponent<DraggableItem>();
            if (d == null) continue;
            if (d.CurrentSlot == null) continue;
            items.Add(d);
        }

        for (int i = 0; i < items.Count; i++)
        {
            var d = items[i];
            var iv = d.GetComponent<ItemView>();
            if (iv == null) continue;

            for (int s = 0; s < emptySlots.Count; s++)
            {
                var dst = emptySlots[s];
                if (WouldMakeTripleOnShelfAfterMove(iv, d.CurrentSlot, dst))
                {
                    fromItem = d;
                    targetSlot = dst;
                    return true;
                }
            }
        }

        return false;
    }

    bool WouldMakeTripleOnShelfAfterMove(ItemView movingItem, SlotView from, SlotView to)
    {
        var shelf = to.shelf;
        if (shelf == null || shelf.slots == null || shelf.slots.Count != 3) return false;

        var t0 = GetTypeAfterMove(shelf.slots[0], movingItem, from, to);
        var t1 = GetTypeAfterMove(shelf.slots[1], movingItem, from, to);
        var t2 = GetTypeAfterMove(shelf.slots[2], movingItem, from, to);

        if (!t0.HasValue || !t1.HasValue || !t2.HasValue) return false;
        return t0.Value == t1.Value && t1.Value == t2.Value;
    }

    ItemType? GetTypeAfterMove(SlotView slot, ItemView moving, SlotView from, SlotView to)
    {
        if (slot == to) return moving.Type;   // в слот назначения кладём moving
        if (slot == from) return null;        // из исходного слота уходим → пусто

        var iv = slot.GetItemView();
        if (iv == null || iv.IsClearing) return null;
        return iv.Type;
    }
    void ShowFinger(DraggableItem item, SlotView slot)
    {
        if (hintFinger == null) return;

        var canvas = this.canvas != null ? this.canvas : FindFirstObjectByType<Canvas>();
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 itemScreen = RectTransformUtility.WorldToScreenPoint(cam, item.transform.position);
        Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slot.transform.position);

        RectTransform canvasRt = (RectTransform)canvas.transform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, itemScreen, cam, out var fromLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, slotScreen, cam, out var toLocal);

        // ✅ смещение цели (вниз от центра слота)
        toLocal += hintToOffset;


        hintFinger.gameObject.SetActive(true);
        hintFinger.anchoredPosition = fromLocal;

        hintFinger.DOKill();
        hintFinger.DOAnchorPos(toLocal, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }
    void HideTutorialFinger()
    {
        if (hintFinger == null) return;
        hintFinger.DOKill();
        hintFinger.gameObject.SetActive(false);
    }
    void ApplyLayoutFromLevel()
    {
        if (layoutRoots == null || layoutRoots.Length == 0)
        {
            Debug.LogError("BoardManager: layoutRoots не назначен");
            return;
        }

        int idx = Mathf.Clamp(
                currentLayoutIndexOverride.HasValue ? currentLayoutIndexOverride.Value : (level != null ? level.layoutIndex : 0),
                0, layoutRoots.Length - 1);

        for (int i = 0; i < layoutRoots.Length; i++)
            layoutRoots[i].gameObject.SetActive(i == idx);

        activeLayoutRoot = layoutRoots[idx];

        shelves.Clear();
        shelves.AddRange(activeLayoutRoot.GetComponentsInChildren<ShelfView>(true));

        // на всякий — чтобы порядок был стабильный
        shelves.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        Canvas.ForceUpdateCanvases();
    }
    Dictionary<ItemType, int> CountStartTypes()
    {
        var counts = new Dictionary<ItemType, int>();

        foreach (var sh in shelves)
            foreach (var sl in sh.slots)
            {
                var iv = sl.GetItemView();
                if (iv == null || iv.IsClearing) continue;

                if (!counts.TryGetValue(iv.Type, out var c)) c = 0;
                counts[iv.Type] = c + 1;
            }

        return counts;
    }
    void BuildBalancedBackBag_AsPacks(ItemType[] types, int totalPacks)
    {
        globalBackBag.Clear();

        int totalItems = totalPacks * 3;
        var startCounts = CountStartTypes();

        // ===== 1) Сначала "добигаем" остатки старта до кратности 3 =====
        var backCounts = new Dictionary<ItemType, int>(types.Length);
        int used = 0;

        foreach (var t in types)
        {
            startCounts.TryGetValue(t, out int s);
            int need = (3 - (s % 3)) % 3; // 0..2
            backCounts[t] = need;
            used += need;
        }

        // Если need не влезает в totalItems (редко: когда допов слишком мало) — fallback
        if (used > totalItems)
        {
            backCounts.Clear();
            used = 0;
            foreach (var t in types) backCounts[t] = 0;
        }

        int remaining = totalItems - used;

        // ===== 2) Остаток добавляем блоками по 3 (это сохраняет кратность) =====
        int ti = 0;
        while (remaining > 0)
        {
            backCounts[types[ti]] += 3;
            remaining -= 3;
            ti = (ti + 1) % types.Length;
        }

        // ===== 3) Теперь собираем пачки по 3, стараясь избегать AAA =====
        // Список типов, где лежат "оставшиеся" штуки
        var pool = new List<ItemType>();
        foreach (var kv in backCounts)
            for (int i = 0; i < kv.Value; i++)
                pool.Add(kv.Key);

        // немного перемешаем пул, чтобы пачки не были одинаковыми
        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // Утилита: взять один элемент типа t из pool (первое вхождение)
        bool TakeOne(ItemType t, List<ItemType> list, out ItemType taken)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (EqualityComparer<ItemType>.Default.Equals(list[i], t))
                {
                    taken = list[i];
                    list.RemoveAt(i);
                    return true;
                }
            }
            taken = default;
            return false;
        }

        // Утилита: взять любой элемент, но желательно НЕ equal forbidden
        bool TakeAnyNot(ItemType forbidden, List<ItemType> list, out ItemType taken)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!EqualityComparer<ItemType>.Default.Equals(list[i], forbidden))
                {
                    taken = list[i];
                    list.RemoveAt(i);
                    return true;
                }
            }
            // если не нашли — берём любой
            if (list.Count > 0)
            {
                taken = list[0];
                list.RemoveAt(0);
                return true;
            }
            taken = default;
            return false;
        }

        // Утилита: взять самый частый тип в pool (чтобы разгружать хвосты)
        ItemType GetMostCommon(List<ItemType> list)
        {
            var c = new Dictionary<ItemType, int>();
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                c.TryGetValue(t, out int v);
                c[t] = v + 1;
            }

            ItemType best = list[0];
            int bestCount = -1;
            foreach (var kv in c)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    best = kv.Key;
                }
            }
            return best;
        }

        // Собираем totalPacks пачек
        for (int p = 0; p < totalPacks; p++)
        {
            if (pool.Count < 3)
                break;

            // Берём первый тип как "якорь" — обычно самый частый, чтобы не оставлять хвосты
            ItemType a = GetMostCommon(pool);
            TakeOne(a, pool, out a);

            // b: берём любой НЕ a, если возможно
            ItemType b;
            if (!TakeAnyNot(a, pool, out b))
                break;

            // c: если a==b (то есть мы взяли второй a), то c стараемся взять не a (анти-AAA)
            ItemType c;
            if (EqualityComparer<ItemType>.Default.Equals(a, b))
            {
                // хотим НЕ a, чтобы не получился AAA
                if (!TakeAnyNot(a, pool, out c))
                    break;
            }
            else
            {
                // если a!=b, то c можно любой
                c = pool[0];
                pool.RemoveAt(0);
            }

            // Если всё равно вышло AAA (может быть, если pool весь из a) — принимаем как есть
            globalBackBag.Enqueue(a);
            globalBackBag.Enqueue(b);
            globalBackBag.Enqueue(c);
        }

        // Если вдруг что-то осталось (не должно, но на всякий) — просто докидываем
        while (pool.Count > 0)
        {
            globalBackBag.Enqueue(pool[0]);
            pool.RemoveAt(0);
        }
    }
    bool TryDrawPack(out ItemType a, out ItemType b, out ItemType c)
    {
        a = b = c = default;

        if (globalBackBag.Count < 3)
            return false;

        a = globalBackBag.Dequeue();
        b = globalBackBag.Dequeue();
        c = globalBackBag.Dequeue();
        return true;
    }
    public void DebugNextLevel()
    {
        // на всякий: прячем подсказки/панели
        HideTutorialFinger();
        if (losePanel != null) losePanel.Hide();
        if (winPanel != null) winPanel.Hide();

        // стопаем корутины, чтобы старые рутины не мешали
        StopAllCoroutines();

        // убираем текущие предметы
        ClearAllSlots();

        // увеличиваем уровень
        GameFlow.AdvanceGameLevel();
        Debug.Log($"[DEBUG] NEXT LEVEL -> GameLevel={GameFlow.GameLevel}");

        // грузим конфиг по индексу
        LoadLevelByGameLevel(GameFlow.GameLevel);

        // (опционально) заново запускаем стартовые корутины
        StartCoroutine(InitialRefillWithFailsafe());
        StartCoroutine(TutorialHint_Level1());
    }

    public void DebugPrevLevel()
    {
        HideTutorialFinger();
        if (losePanel != null) losePanel.Hide();
        if (winPanel != null) winPanel.Hide();

        StopAllCoroutines();
        ClearAllSlots();

        GameFlow.GameLevel = Mathf.Max(1, GameFlow.GameLevel - 1);
        Debug.Log($"[DEBUG] PREV LEVEL -> GameLevel={GameFlow.GameLevel}");

        LoadLevelByGameLevel(GameFlow.GameLevel);

        StartCoroutine(InitialRefillWithFailsafe());
        StartCoroutine(TutorialHint_Level1());
    }

    int GetLoopedVisualIndex(int gameLevel)
    {
        int idx = gameLevel - 1;

        if (!loopVisuals)
            return Mathf.Clamp(idx, 0, levels.Length - 1);

        int fromIdx = Mathf.Clamp(loopFromLevel - 1, 0, levels.Length - 1);
        int toIdx = Mathf.Clamp(loopToLevel - 1, 0, levels.Length - 1);
        if (toIdx < fromIdx) (fromIdx, toIdx) = (toIdx, fromIdx);

        if (idx < fromIdx)
            return Mathf.Clamp(idx, 0, levels.Length - 1);

        int len = (toIdx - fromIdx) + 1;
        int offset = (idx - fromIdx) % len;
        if (offset < 0) offset += len;

        return fromIdx + offset;
    }
    void SetupLevelForGameLevel(int gameLevel)
    {
        int cfgIdx = Mathf.Clamp(gameLevel - 1, 0, levels.Length - 1);
        int visualIdx = GetLoopedVisualIndex(gameLevel); // твой метод лупа 18..25

        var cfg = levels[cfgIdx];
        var visualSrc = levels[visualIdx];

        level = cfg;

        // если ты делал override визуала/лейаута:
        currentVisualsOverride = visualSrc != null ? visualSrc.visuals : null;
        currentLayoutIndexOverride = visualSrc != null ? visualSrc.layoutIndex : (int?)null;

        //Debug.Log($"SETUP: GL={gameLevel} cfgIdx={cfgIdx} ({cfg.name}) visualIdx={visualIdx} ({visualSrc.name})");
    }
    int CountEmptySlots()
    {
        int count = 0;
        foreach (var sh in shelves)
            foreach (var sl in sh.slots)
                if (sl.GetItemView() == null) count++;
        return count;
    }
    public bool TryOpenModal()
    {
        if (ModalOpen) return false;
        ModalOpen = true;
        return true;
    }

    public void CloseModal()
    {
        ModalOpen = false;
    }
    void TryShowRewardPopup()
    {
        if (debugLog) Debug.Log($"RewardPopup: queued={rewardQueued} panel={(rewardPanel != null)} idle={!AnyShelfClearing() && !AnyItemClearing()} modalOpen={ModalOpen}");
        if (!rewardQueued) return;
        if (rewardPanel == null) return;

        // показываем только на спокойной доске
        if (AnyShelfClearing() || AnyItemClearing()) return;

        // если уже открыта другая панель — подождём следующего idle
        if (!TryOpenModal()) return;

        rewardQueued = false;

        // ✅ ВАЖНО: настроить текст/награду ДО Show
        int coins = GetRewardCoinsNow(); // твоя формула/прогрессия от уровня
        int xp = 0;
        int score = 0;

        rewardPanel.Configure(coins, xp, score, title: "Награда!");

        rewardPanel.Show(() =>
        {
            CloseModal();
        });
    }
    int GetRewardCoinsNow()
    {
        int gl = GameFlow.GameLevel;

        int baseCoins = 30;    // сколько на 1 уровне
        int stepEvery = 2;     // шаг уровней
        int addPerStep = 10;   // прибавка за шаг
        int maxCoins = 400;    // (опционально) потолок

        int steps = (gl - 1) / stepEvery; // 1-5 =>0, 6-10=>1...
        int coins = baseCoins + steps * addPerStep;

        return Mathf.Min(coins, maxCoins);
    }
    int GetTriplesToRewardNow()
    {
        int gl = GameFlow.GameLevel;

        if (gl <= 5) return 10;   // чаще
        if (gl <= 10) return 18;   // средне
        if (gl <= 20) return 24;   // средне
        return 28;                 // реже
    }
    public int GetRemovableTriplets(int desiredTriplets)
    {
        desiredTriplets = Mathf.Max(1, desiredTriplets);

        // живые предметы
        var snapshot = new List<ItemView>(allItems);
        snapshot.RemoveAll(v => v == null || v.IsClearing);

        if (snapshot.Count < 3)
            return 0;

        // считаем по типам
        var counts = new Dictionary<ItemType, int>();
        foreach (var it in snapshot)
        {
            counts.TryGetValue(it.Type, out int c);
            counts[it.Type] = c + 1;
        }

        // сколько троек можно снять с ОДНОГО типа (как делает твой бафф)
        int maxTripletsSameType = 0;
        foreach (var kv in counts)
            maxTripletsSameType = Mathf.Max(maxTripletsSameType, kv.Value / 3);

        if (maxTripletsSameType <= 0)
            return 0;

        // приоритет desired, но если не получается — максимум, но хотя бы 1
        return Mathf.Clamp(desiredTriplets, 1, maxTripletsSameType);
    }
    public void OnBoostUsed()
    {
        HideTutorialFinger();
    }
}
