using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CollectionDatabase db;
    [SerializeField] private int triplesToPopup = 3;   // окно каждые N троек
    [SerializeField] private float step = 0.15f;       // +15% за сбор
    [SerializeField] private bool showOnlyWhenBoardIdle = true;

    [Header("UI")]
    [SerializeField] private CollectionPopupUI popup;  // ссылка на UI
    

    private CollectionSave save;
    private const string SaveKey = "COLLECTION_SAVE_V1";

    // Твой источник состояния доски. Заменишь на свой.
    public System.Func<bool> IsBoardIdle; // назначишь из Board/Match3Manager

    private bool popupQueued;

    public int ItemsCount => (db != null && db.items != null) ? db.items.Count : 0;


    private void Awake()
    {
        //PlayerPrefs.DeleteAll();
        LoadOrCreate();
        if (popup == null) return;
        popup.HideImmediate();
        popup.OnCollectClicked = CollectStep;
    }

    // Вызывай это каждый раз, когда тройка собрана и удалена
    public void OnTripleMatched()
    {
        if (IsCollectionComplete()) return;

        save.pendingTriples++;

        if (save.pendingTriples >= triplesToPopup)
            popupQueued = true;

        Save();
        TryShowPopup();
    }

    // Вызывай это, когда борд стал idle (после падений/каскадов)
    public void OnBoardBecameIdle()
    {
        TryShowPopup();
    }

    private void TryShowPopup()
    {
        if (!popupQueued) return;
        if (IsCollectionComplete()) { popupQueued = false; return; }

        if (showOnlyWhenBoardIdle && IsBoardIdle != null && !IsBoardIdle())
            return;

        // показываем окно
        popupQueued = false;
        ShowPopupForCurrentItem();
    }

    private void ShowPopupForCurrentItem()
    {
        int i = save.currentIndex;
        var item = db.items[i];

        float current = save.progress[i];
        float target = Mathf.Clamp01(current + step);

        popup.ShowPreview(item.sprite, current, target, step);
    }

    private void CollectStep()
    {
        if (IsCollectionComplete()) { popup.Hide(); return; }

        save.pendingTriples = Mathf.Max(0, save.pendingTriples - triplesToPopup);

        int i = save.currentIndex;

        float current = save.progress[i];
        float target = Mathf.Clamp01(current + step);

        // Сохраняем уже ПОСЛЕ клика
        save.progress[i] = target;
        Save();

        // если дошли до 100 — запускаем эффект завершения
        if (target >= 1f - 0.0001f)
        {
            StartCoroutine(CompleteItemFlow());
            return;
        }

        popup.Hide();

        if (save.pendingTriples >= triplesToPopup)
            popupQueued = true;

        TryShowPopup();
    }

    public bool IsCollectionComplete()
    {
        return db == null || db.items == null || save.currentIndex >= db.items.Count;
    }

    public float GetItemProgress(int index) => save.progress[index];
    public bool IsItemUnlocked(int index) => save.progress[index] >= 1f;

    private void LoadOrCreate()
    {
        if (db == null) { save = new CollectionSave { currentIndex = 0, progress = new float[0], pendingTriples = 0 }; return; }

        if (PlayerPrefs.HasKey(SaveKey))
        {
            save = JsonUtility.FromJson<CollectionSave>(PlayerPrefs.GetString(SaveKey));
        }
        else
        {
            save = new CollectionSave();
        }

        // защита от изменения количества предметов
        int n = db.items.Count;
        if (save.progress == null || save.progress.Length != n)
        {
            var newProgress = new float[n];
            if (save.progress != null)
            {
                int copy = Mathf.Min(save.progress.Length, n);
                for (int i = 0; i < copy; i++) newProgress[i] = save.progress[i];
            }
            save.progress = newProgress;
        }


        save.currentIndex = Mathf.Clamp(save.currentIndex, 0, n);
        save.pendingTriples = Mathf.Max(0, save.pendingTriples);
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }
    IEnumerator CompleteItemFlow()
    {
        // 🎉 ЭФФЕКТ "ПРЕДМЕТ СОБРАН"
        yield return popup.PlayCompleteEffectDOTween();

        // ✔ теперь считаем предмет собранным
        save.currentIndex++;

        Save();

        popup.Hide();

        // если накопились тройки — попап покажется позже
        if (save.pendingTriples >= triplesToPopup)
            popupQueued = true;

        TryShowPopup();
    }
    public bool TryGetItem(int index, out CollectionItem item)
    {
        item = null;
        if (db == null || db.items == null) return false;
        if (index < 0 || index >= db.items.Count) return false;
        item = db.items[index];
        return true;
    }

    public bool IsUnlockedById(string id)
    {
        if (db == null || db.items == null || save?.progress == null) return false;

        for (int i = 0; i < db.items.Count; i++)
            if (db.items[i].id == id)
                return save.progress[i] >= 1f;

        return false;
    }

    public List<int> GetUnlockedIndexes()
    {
        var result = new List<int>();
        if (db == null || db.items == null || save?.progress == null) return result;

        for (int i = 0; i < db.items.Count; i++)
            if (save.progress[i] >= 1f)
                result.Add(i);

        return result;
    }
}
