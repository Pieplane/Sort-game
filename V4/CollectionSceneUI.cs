using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionSceneUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CollectionManager collectionManager;
    [SerializeField] private ScrollRect scrollRect;      // ✅ добавь
    [SerializeField] private RectTransform content;       // ✅ лучше RectTransform
    [SerializeField] private CollectionCardUI cardPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyText;

    [Header("Focus")]
    [SerializeField] private bool focusOnLastProgressOrUnlocked = true;
    [SerializeField] private float focusDelayFrames = 2; // иногда надо 1-2 кадра, чтобы Layout обновился

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    void Start()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        // очистка
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (collectionManager == null)
            return;

        int total = collectionManager.ItemsCount;
        int added = 0;

        // построим список карточек, чтобы потом сфокусироваться
        // (индекс в этом списке != индекс в базе, поэтому храним mapping)
        var builtCards = new System.Collections.Generic.List<RectTransform>();
        var builtDbIndexes = new System.Collections.Generic.List<int>();

        for (int i = 0; i < total; i++)
        {
            float progress = collectionManager.GetItemProgress(i);
            if (progress <= 0f) continue;

            if (!collectionManager.TryGetItem(i, out var item))
                continue;

            var card = Instantiate(cardPrefab, content);
            card.name = $"CollectionCard_{item.id}";
            card.Set(item.sprite, progress);

            builtCards.Add(card.GetComponent<RectTransform>());
            builtDbIndexes.Add(i);

            added++;
        }

        if (emptyText != null)
            emptyText.SetActive(added == 0);

        if (debugLog)
            Debug.Log($"[CollectionSceneUI] Rebuild: items shown = {added}");

        if (added == 0) return;

        if (focusOnLastProgressOrUnlocked && scrollRect != null)
        {
            int focusBuiltIndex = FindFocusBuiltIndex(builtDbIndexes);
            if (focusBuiltIndex >= 0 && focusBuiltIndex < builtCards.Count)
                StartCoroutine(FocusAfterLayout(builtCards[focusBuiltIndex]));
        }
    }

    // Выбираем: последний "открывающийся" (0..1), иначе последний "открытый" (==1)
    private int FindFocusBuiltIndex(System.Collections.Generic.List<int> builtDbIndexes)
    {
        int lastInProgress = -1;
        int lastUnlocked = -1;

        for (int k = 0; k < builtDbIndexes.Count; k++)
        {
            int dbIndex = builtDbIndexes[k];
            float p = collectionManager.GetItemProgress(dbIndex);

            if (p > 0f && p < 1f) lastInProgress = k;
            if (p >= 1f) lastUnlocked = k;
        }

        int focus = (lastInProgress != -1) ? lastInProgress : lastUnlocked;

        if (debugLog)
            Debug.Log($"[CollectionSceneUI] Focus builtIndex={focus} (inProgress={lastInProgress}, unlocked={lastUnlocked})");

        return focus;
    }

    private System.Collections.IEnumerator FocusAfterLayout(RectTransform targetCard)
    {
        // ждём обновление LayoutGroup/ContentSizeFitter
        int frames = Mathf.Max(0, Mathf.RoundToInt(focusDelayFrames));
        for (int i = 0; i < frames; i++)
            yield return null;

        Canvas.ForceUpdateCanvases();

        CenterOn(targetCard);
    }

    private void CenterOn(RectTransform target)
    {
        // Работает для горизонтального scrollRect
        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        if (contentWidth <= viewportWidth)
        {
            // нечего скроллить
            scrollRect.horizontalNormalizedPosition = 0.5f;
            return;
        }

        // Позиция центра target в локальных координатах content (учитывая pivot/якоря)
        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Vector3 localCenter = content.InverseTransformPoint(worldCenter);

        // localCenter.x считается относительно pivot content.
        // Переводим в "отступ слева" (0..contentWidth)
        float centerFromLeft = localCenter.x + contentWidth * content.pivot.x;

        // Хотим, чтобы центр target оказался в центре viewport:
        float desiredLeft = centerFromLeft - viewportWidth * 0.5f;

        // Clamp в допустимый диапазон
        float maxLeft = contentWidth - viewportWidth;
        desiredLeft = Mathf.Clamp(desiredLeft, 0f, maxLeft);

        float normalized = desiredLeft / maxLeft;
        scrollRect.horizontalNormalizedPosition = normalized;

        if (debugLog)
            Debug.Log($"[CollectionSceneUI] CenterOn: contentW={contentWidth:F1}, viewportW={viewportWidth:F1}, desiredLeft={desiredLeft:F1}, norm={normalized:F3}");
    }
}
