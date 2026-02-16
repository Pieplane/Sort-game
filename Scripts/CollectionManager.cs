using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CollectionDatabase db;
    [SerializeField] private float step = 0.15f;
    [SerializeField] private bool showOnlyWhenBoardIdle = true;

    [Header("UI")]
    [SerializeField] private CollectionPopupUI popup;

    // ✅ теперь используем только CollectionSaveData
    private CollectionSaveData save;

    public System.Func<bool> IsBoardIdle;
    private bool popupQueued;

    public int ItemsCount => (db != null && db.items != null) ? db.items.Count : 0;

    public System.Func<bool> TryOpenModal;
    public System.Action CloseModal;
    IEnumerator Start()
    {
        //PlayerPrefs.DeleteAll();
        // ждём, пока появится PlayerProgress
        while (PlayerProgress.Instance == null)
            yield return null;

        // гарантированно подтягиваем сохранение (и в Editor, и в WebGL после облака)
        TryInitFromProgress();
        //DebugPrintProgress();
    }

    void Awake()
    {
        if (popup != null)
        {
            popup.HideImmediate();
            popup.OnCollectClicked = () =>
            {
                AdController.Instance.ShowAd(() =>
                {
                    CollectStep();
                });
            };
        }
    }

    void OnEnable()
    {
        // ✅ если WebGL-облако грузится асинхронно — подхватим после загрузки
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLoaded += OnProgressLoaded;
    }

    void OnDisable()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLoaded -= OnProgressLoaded;
    }

    void OnProgressLoaded()
    {
        TryInitFromProgress();
    }

    void TryInitFromProgress()
    {
        if (db == null || db.items == null)
        {
            save = new CollectionSaveData { currentIndex = 0, pendingTriples = 0, progress = new float[0] };
            return;
        }

        int n = db.items.Count;

        // берём из PlayerProgress (если нет — создаём дефолт)
        var pp = PlayerProgress.Instance;
        if (pp == null || pp.Collection == null)
        {
            save = new CollectionSaveData { currentIndex = 0, pendingTriples = 0, progress = new float[n] };
            return;
        }

        save = pp.Collection;

        // ✅ защита на случай изменения количества предметов
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

    // ✅ теперь сохраняем через общий прогресс
    void Save()
    {
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.SaveCollection(save);
    }

    // Вызывай это каждый раз, когда тройка собрана и удалена
    public void OnTripleMatched()
    {
        if (IsCollectionComplete()) return;

        save.pendingTriples++;

        int need = GetTriplesToPopupNow();
        if (save.pendingTriples >= need)
            popupQueued = true;

        Save();
        TryShowPopup();
    }

    public void OnBoardBecameIdle()
    {
        TryShowPopup();
    }

    void TryShowPopup()
    {
        if (!popupQueued) return;
        if (IsCollectionComplete()) { popupQueued = false; return; }

        if (showOnlyWhenBoardIdle && IsBoardIdle != null && !IsBoardIdle())
            return;

        if (TryOpenModal != null && !TryOpenModal())
            return;

        popupQueued = false;
        ShowPopupForCurrentItem();
    }

    void ShowPopupForCurrentItem()
    {
        int i = save.currentIndex;
        var item = db.items[i];

        float current = save.progress[i];
        float target = Mathf.Clamp01(current + step);

        popup.ShowPreview(item.sprite, current, target, step);
    }

    void CollectStep()
    {
        if (IsCollectionComplete()) { popup.Hide(); return; }

        AudioManager.Instance?.Play("Click");

        int need = GetTriplesToPopupNow();
        save.pendingTriples = Mathf.Max(0, save.pendingTriples - need);

        int i = save.currentIndex;

        float current = save.progress[i];
        float target = Mathf.Clamp01(current + step);

        save.progress[i] = target;
        Save();

        if (target >= 1f - 0.0001f)
        {
            StartCoroutine(CompleteItemFlow());
            return;
        }

        popup.Hide();
        CloseModal?.Invoke();

        if (save.pendingTriples >= GetTriplesToPopupNow())
            popupQueued = true;

        TryShowPopup();
    }

    IEnumerator CompleteItemFlow()
    {
        yield return popup.PlayCompleteEffectDOTween();

        save.currentIndex++;
        Save();

        popup.Hide();
        CloseModal?.Invoke();

        if (save.pendingTriples >= GetTriplesToPopupNow())
            popupQueued = true;

        TryShowPopup();
    }

    public bool IsCollectionComplete()
    {
        return db == null || db.items == null || save == null || save.currentIndex >= db.items.Count;
    }

    public float GetItemProgress(int index) => (save?.progress != null && index >= 0 && index < save.progress.Length) ? save.progress[index] : 0f;
    public bool IsItemUnlocked(int index) => GetItemProgress(index) >= 1f;

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

    int GetTriplesToPopupNow()
    {
        int gl = GameFlow.GameLevel;
        if (gl <= 5) return 8;
        if (gl <= 10) return 12;
        if (gl <= 20) return 16;
        return 20;
    }
    //public void DebugPrintProgress()
    //{
    //    string key = "pp_data_v1"; // или твой K_LOCAL

    //    if (!PlayerPrefs.HasKey(key))
    //    {
    //        Debug.Log($"[PP] PlayerPrefs key '{key}' NOT FOUND");
    //        return;
    //    }

    //    string json = PlayerPrefs.GetString(key);
    //    Debug.Log($"[PP] {key} = {json}");
    //}
}
