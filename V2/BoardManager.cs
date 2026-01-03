using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
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

    struct Pack
    {
        public ItemType a, b, c;
        public Pack(ItemType a, ItemType b, ItemType c) { this.a = a; this.b = b; this.c = c; }
    }

    private readonly Dictionary<ShelfView, Pack> reservedPack = new Dictionary<ShelfView, Pack>();

    void Awake()
    {
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
    }

    void Start()
    {
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

        SpawnLevel();
        InitBackSystem();

        // ✅ если какая-то полка пустая на старте — сразу выдвигаем допы
        StartCoroutine(InitialRefillIfNeeded());
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
        // 1) чистим тройки и считаем
        int triples = ResolveAllTriples_Count();

        // 2) начисляем
        if (triples > 0 && PlayerProgress.Instance != null)
        {
            int combo = Mathf.Max(1, triples);
            PlayerProgress.Instance.AddClear(triples, combo);
        }

        // 3) единая пост-обработка: дождаться анимаций, выдвинуть допы, цепочки, луз-чек
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
    }

    // ===================== UNIVERSAL POST =====================

    IEnumerator AfterBoardChangedRoutine()
    {
        // ✅ ждём и shelf-clears (если они есть), и item-clears (баффы/анимации)
        yield return new WaitUntil(() => !AnyShelfClearing() && !AnyItemClearing());

        // ✅ кадр, чтобы Destroy реально убрал объекты из иерархии
        yield return null;

        // ✅ теперь слоты реально пустые -> допы выедут сразу
        RefillEmptyShelvesNow();

        // ✅ цепочки (если после выезда собралась тройка)
        for (int guard = 0; guard < 10; guard++)
        {
            int triples = ResolveAllTriples_Count();
            if (triples <= 0) break;

            yield return new WaitUntil(() => !AnyShelfClearing() && !AnyItemClearing());
            yield return null;

            RefillEmptyShelvesNow();
        }

        // ✅ win
        if (CountAllItems() == 0)
        {
            HandleLevelComplete();
            yield break;
        }

        // ✅ lose
        if (!HasAnyEmptySlot()) GameOver();
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
                {
                    clearedThisPass = true;
                    triplesTotal++;
                }
            }

            if (!clearedThisPass) break;
        }

        if (triplesTotal > 0 && debugLog)
            Debug.Log($"✅ Тройки удалены: {triplesTotal}");

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

    int CountAllItems()
    {
        return FindObjectsOfType<ItemView>().Length;
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
        Debug.Log("💀 GAME OVER: нет свободных слотов");
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

        var all = new List<ItemView>(FindObjectsOfType<ItemView>());
        if (all.Count == 0) return;

        var groups = new Dictionary<ItemType, List<ItemView>>();

        foreach (var it in all)
        {
            if (it == null || it.IsClearing) continue;
            if (!groups.ContainsKey(it.Type)) groups[it.Type] = new List<ItemView>();
            groups[it.Type].Add(it);
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

        var list = groups[chosen];

        for (int i = 0; i < removeCount; i++)
        {
            var item = list[i];
            if (item == null || item.IsClearing) continue;

            item.MarkClearing();
            StartCoroutine(item.PlayClearPopCollapse()); // Destroy внутри корутины
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
        List<ItemView> items = new List<ItemView>(FindObjectsOfType<ItemView>());
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
    bool AnyItemClearing()
    {
        var items = FindObjectsOfType<ItemView>();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].IsClearing)
                return true;
        }
        return false;
    }
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
        Debug.Log("🎉 LEVEL COMPLETE!");

        if (PlayerProgress.Instance != null)
        {
            int reward = PlayerProgress.Instance.GetCoinsForLevelComplete();
            PlayerProgress.Instance.AddCoins(reward);
            Debug.Log($"💰 +{reward} coins");
        }

        // тут позже покажешь UI победы / кнопку Next
    }
}
