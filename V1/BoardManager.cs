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

    private readonly List<ShelfView> shelves = new List<ShelfView>();

    void Awake()
    {
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
    }

    void Start()
    {
        // Найти все полки в сцене
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
    }

    public void SpawnLevel()
    {
        int totalSlots = GetTotalSlots();
        int emptyTotal = GetEmptyTotal(totalSlots);

        const int maxAttempts = 500;

        List<ItemTypeOrEmpty> best = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var layout = BuildLayoutMultipleOf3(totalSlots, emptyTotal);

            // ✅ проверяем ТОЛЬКО по layout, без Instantiate/Destroy
            if (!HasTripleInLayout(layout))
            {
                best = layout;
                // Debug.Log($"SpawnLevel OK attempt {attempt+1}");
                break;
            }
        }

        if (best == null)
        {
            Debug.LogWarning("SpawnLevel: не смог подобрать старт без троек за maxAttempts, оставляю как есть.");
            best = BuildLayoutMultipleOf3(totalSlots, emptyTotal);
        }

        // ✅ очищаем и спавним ОДИН раз
        ClearAllSlots();
        ApplyLayout(best);
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

            // берём 3 клетки, которые попадут в эту полку
            var c0 = layout[index + 0];
            var c1 = layout[index + 1];
            var c2 = layout[index + 2];

            // если все три НЕ пустые и одинаковые — это стартовая тройка
            if (c0 != ItemTypeOrEmpty.Empty &&
                c0 == c1 && c1 == c2)
                return true;

            index += shelf.slots.Count;
        }

        return false;
    }

    // Эти методы оставляем, чтобы DraggableItem не ругался
    public void RegisterDragging(DraggableItem item) { }
    public void UnregisterDragging(DraggableItem item) { }

    // Вызывается из SlotView после успешного дропа
    public void OnItemPlaced(SlotView slot)
    {
        ResolveAllTriples();

        if (CountAllItems() == 0)
        {
            Debug.Log("🎉 LEVEL COMPLETE!");
            // позже: показать экран победы/кнопку next
        }
    }

    // -------------------- Core helpers --------------------

    int GetTotalSlots()
    {
        int total = 0;
        foreach (var sh in shelves)
        {
            total += sh.slots.Count; // у тебя должно быть 3
        }
        return total;
    }

    int GetEmptyTotal(int totalSlots)
    {
        // Разрешаем 0..totalSlots-1
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

    // Удаляем тройки "до упора" (на всех полках)
    void ResolveAllTriples()
    {
        bool anyClearedOverall = false;

        // защитный лимит на случай ошибок, чтобы не уйти в бесконечный цикл
        for (int guard = 0; guard < 20; guard++)
        {
            bool clearedThisPass = false;

            foreach (var sh in shelves)
            {
                if (sh.TryClearTriple())
                    clearedThisPass = true;
            }

            if (!clearedThisPass) break;
            anyClearedOverall = true;
        }

        if (anyClearedOverall)
            Debug.Log("✅ Тройки удалены");
    }

    int CountAllItems()
    {
        // Самый простой способ: посчитать ItemView в сцене
        // (для маленькой сцены ок, позже оптимизируем)
        return FindObjectsOfType<ItemView>().Length;
    }

    // -------------------- Layout generation (no leftovers) --------------------

    List<ItemTypeOrEmpty> BuildLayoutMultipleOf3(int totalSlots, int emptyTotal)
    {
        int itemsCount = totalSlots - emptyTotal;

        // Если itemsCount не кратно 3, то остатки неизбежны.
        // Поэтому добавляем пустые, пока itemsCount не станет кратно 3.
        while (itemsCount % 3 != 0 && emptyTotal < totalSlots - 1)
        {
            emptyTotal++;
            itemsCount = totalSlots - emptyTotal;
        }
        Debug.Log("itemsCount " + itemsCount);

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

        // перемешать
        for (int i = 0; i < layout.Count; i++)
        {
            int j = Random.Range(i, layout.Count);
            (layout[i], layout[j]) = (layout[j], layout[i]);
        }

        return layout;
    }
}
