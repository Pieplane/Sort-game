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
    public ItemVisualConfig visuals;

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

    public int ItemsCount => allItems.Count;
    bool AnyItemClearing() => activeClearingItems > 0;
    int CountAllItems() => allItems.Count;
    //public bool AnyItemClearingFast => activeClearingItems > 0;

    //bool AnyItemClearing() => activeClearingItems > 0;

    //int CountAllItems() => allItems.Count;


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

    void Start()
    {
        // прогрев DOTween, чтобы первый реальный tween не лагал
        DOTween.To(() => 0f, x => { }, 1f, 0.01f).SetUpdate(true).Kill();

        // прогрев UI rebuild (часто помогает, если есть layout группы)
        Canvas.ForceUpdateCanvases();

        shelves.Clear();
        shelves.AddRange(FindObjectsOfType<ShelfView>());

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

        if (visuals == null)
        {
            Debug.LogError("BoardManager: Не назначен ItemVisualConfig (visuals).");
            return;
        }
        if (collectionManager != null)
        {
            collectionManager.IsBoardIdle = () =>
                !AnyShelfClearing() && !AnyItemClearing();
        }
        if (losePanel != null)
        {
            losePanel.OnRestart = () =>
            {
                losePanel.Hide();
                RestartLevel();
            };

            losePanel.OnContinue = ContinueAfterLose;
        }

        SpawnLevel();
        InitBackSystem();

        // ✅ если какая-то полка пустая на старте — сразу выдвигаем допы
        //StartCoroutine(InitialRefillIfNeeded());
        StartCoroutine(InitialRefillWithFailsafe());
    }

    // Эти методы нужны DraggableItem (оставляем)
    public void RegisterDragging(DraggableItem item) { }
    public void UnregisterDragging(DraggableItem item) { }

    // Вызывается из SlotView после успешного дропа
    public void OnItemPlaced(SlotView slot)
    {
        StartCoroutine(AfterMoveRoutine());
    }

    IEnumerator AfterMoveRoutine()
    {
        //// 1) чистим тройки и считаем
        //int triples = ResolveAllTriples_Count();

        //// 2) начисляем
        //if (triples > 0 && PlayerProgress.Instance != null)
        //{
        //    int combo = Mathf.Max(1, triples);
        //    PlayerProgress.Instance.AddClear(triples, combo);
        //}

        //// 3) единая пост-обработка: дождаться анимаций, выдвинуть допы, цепочки, луз-чек
        //yield return StartCoroutine(AfterBoardChangedRoutine());
        if (levelCompleted) yield break;

        int triples = ResolveAllTriples_Count();

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

        if (collectionManager != null)
            collectionManager.OnBoardBecameIdle();

        if (CountAllItems() == 0)
        {
            HandleLevelComplete();
            yield break;
        }

        if (!HasAnyEmptySlot())
        {
            GameOver();
        }
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

        int totalItems = totalPacks * 3;
        var temp = new List<ItemType>(totalItems);

        // Простой и стабильный вариант: равномерно раздаём типы (кратно 3)
        int ti = 0;
        for (int i = 0; i < totalItems; i++)
        {
            temp.Add(types[ti]);
            ti = (ti + 1) % types.Length;
        }

        // перемешать мешок
        for (int i = 0; i < temp.Count; i++)
        {
            int j = Random.Range(i, temp.Count);
            (temp[i], temp[j]) = (temp[j], temp[i]);
        }

        foreach (var it in temp)
            globalBackBag.Enqueue(it);

        if (debugLog)
            Debug.Log($"BackSystem: totalPacks={totalPacks}, totalItems={totalItems}, bag={globalBackBag.Count}");

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

            if (!shelfPacksLeft.TryGetValue(sh, out int left) || left <= 0)
            {
                sh.HideBackPreview();
                continue;
            }

            if (!reservedPack.ContainsKey(sh))
                ReserveNextPackForShelf(sh);

            if (!reservedPack.TryGetValue(sh, out var p))
                continue;

            sh.PlacePack(this, p.a, p.b, p.c);

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

        if (!TryDrawPack_NoAAA(out var a, out var b, out var c))
        {
            sh.HideBackPreview();
            return;
        }

        var p = new Pack(a, b, c);
        reservedPack[sh] = p;

        // затемнённое превью
        sh.ShowBackPreview(visuals, a, b, c);
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
                { clearedThisPass = true; triplesTotal++; }
            }
            if (!clearedThisPass)
                break;
        }
        if (triplesTotal > 0 && debugLog)
            Debug.Log($"✅ Тройки удалены: {triplesTotal}");
        //LogMs("AnyItemClearing", () => { var x = AnyItemClearing(); }); 
        //LogMs("CountAllItems", () => { var x = CountAllItems(); });
        if (triplesTotal > 0 && collectionManager != null)
        { 
            for (int i = 0; i < triplesTotal; i++)
        collectionManager.OnTripleMatched();
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

                iv.SetType((ItemType)cell, visuals);
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

    //int CountAllItems()
    //{
    //    return FindObjectsOfType<ItemView>().Length;
    //}
    //int CountAllItems() => allItems.Count;

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
        //List<ItemView> items = new List<ItemView>(FindObjectsOfType<ItemView>());
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
    //bool AnyItemClearing()
    //{
    //    var items = FindObjectsOfType<ItemView>();
    //    for (int i = 0; i < items.Length; i++)
    //    {
    //        if (items[i] != null && items[i].IsClearing)
    //            return true;
    //    }
    //    return false;
    //}
    //bool AnyItemClearing() => activeClearingItems > 0;
    bool TryDrawPack_NoAAA(out ItemType a, out ItemType b, out ItemType c)
    {
        a = b = c = default;

        if (globalBackBag.Count < 3)
            return false;

        // Переводим очередь в список, чтобы выбрать 3 любые
        var list = new List<ItemType>(globalBackBag);

        // Попробуем до 80 раз найти тройку, которая НЕ AAA
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int i0 = Random.Range(0, list.Count);
            int i1 = Random.Range(0, list.Count);
            int i2 = Random.Range(0, list.Count);

            if (i0 == i1 || i0 == i2 || i1 == i2) continue;

            var x = list[i0];
            var y = list[i1];
            var z = list[i2];

            // не хотим AAA
            if (x == y && y == z) continue;

            // ✅ удаляем выбранные элементы из списка (удалять с конца!)
            int[] idx = { i0, i1, i2 };
            System.Array.Sort(idx);

            a = list[idx[0]];
            b = list[idx[1]];
            c = list[idx[2]];

            list.RemoveAt(idx[2]);
            list.RemoveAt(idx[1]);
            list.RemoveAt(idx[0]);

            // ✅ пересобираем очередь обратно
            globalBackBag.Clear();
            for (int i = 0; i < list.Count; i++)
                globalBackBag.Enqueue(list[i]);

            return true;
        }

        // Если НЕ получилось (например в мешке всё одного типа) — придётся дать AAA
        a = globalBackBag.Dequeue();
        b = globalBackBag.Dequeue();
        c = globalBackBag.Dequeue();
        return true;
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

        // закрываем победную панель быстро
        //if (winPanel != null) winPanel.Hide();
        //multiplierMeter.gameObject.SetActive(false);
        //multiplierUsed = false;

        // ✅ регистрируем оффер
        noAdsOffer?.RegisterLevelWon();

        if (GameFlow.IsOnboarding)
        {
            // ===== УРОВНИ 1-3: начисляем тут и идём на следующий уровень в этой же сцене =====
            ApplyRewardsInstant(coins, score, xp);
            StartCoroutine(RewardSequence(coins, score, xp, delay));
            // можно показать короткий FX (опционально)
            //StartCoroutine(ShortFxSequence());

            // следующий уровень (внутри одной сцены)
            GameFlow.AdvanceGameLevel();
            LoadLevelByGameLevel(GameFlow.GameLevel);

            // оффер можно показывать тут тоже, если надо (обычно после 3-го лучше)
            // noAdsOffer?.TryShowOffer("after_win_onboarding");
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

            noAdsOffer?.TryShowOffer("after_win");
            //if (winPanel != null) winPanel.Hide();

            //// переходим в меню, там полетят FX и начислится
            //SceneManager.LoadScene(menuSceneName);
            StartCoroutine(TransitionToMenuAfterDelay(delay));
        }
    }
    IEnumerator TransitionToMenuAfterDelay(float delay)
    {
        // ⏸ даём доиграть эффектам (звёзды / pop / множитель)
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // 🔒 важно: сначала аккуратно закрыть панель
        if (winPanel != null)
            winPanel.Hide(); // твины убиваются внутри

        // ⏸ один кадр — чтобы DOTween успел всё убить
        yield return null;

        // 🚪 переход в меню
        SceneManager.LoadScene(menuSceneName);
    }
    void LoadLevelByGameLevel(int gameLevel)
    {
        int idx = Mathf.Clamp(gameLevel - 1, 0, levels.Length - 1);
        level = levels[idx];

        // сброс состояния и рестарт
        if (losePanel != null) losePanel.Hide();
        //StopAllCoroutines();
        RestartLevel(); // твой RestartRoutine отработает
    }

    void ApplyRewardsInstant(int coins, int score, int xp)
    {
        if (PlayerProgress.Instance == null) return;

        PlayerProgress.Instance.AddCoins(coins);
        PlayerProgress.Instance.AddScore(score);
        PlayerProgress.Instance.AddXp(xp);
    }
    IEnumerator RewardSequence(int coinsReward, int scoreReward, int xpReward, float delayReward)
    {
        yield return new WaitForSecondsRealtime(delayReward);
        if (winPanel != null) winPanel.Hide();
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
        BuffManager.Instance?.Buff_ClearSomeOfType(3);

        noAdsOffer?.RegisterContinueUsed();
        noAdsOffer?.TryShowOffer("after_continue");

        // после баффа борд сам по себе:
        // - проиграет анимации
        // - вызовет AfterBoardChangedRoutine
        // - продолжит игру
    }
    void EnsurePlayableAtStart()
    {
        // если свободные слоты есть — всё ок
        if (HasAnyEmptySlot())
            return;

        Debug.Log("⚠️ FailSafe: нет свободных слотов на старте, применяем спасение");

        // удаляем ОДНУ тройку (3 предмета)
        Buff_RemoveTripletsOfMostCommonType(1);

        // ⚠️ AfterBoardChangedRoutine внутри уже запустится
    }
    IEnumerator InitialRefillWithFailsafe()
    {
        yield return StartCoroutine(InitialRefillIfNeeded());

        // один кадр, чтобы всё точно встало
        yield return null;

        EnsurePlayableAtStart();
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
}
