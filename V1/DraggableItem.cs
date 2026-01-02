using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Auto refs (can be empty)")]
    public Canvas canvas;                 // можно не ставить вручную
    public CanvasGroup canvasGroup;       // автоподхват
    public RectTransform rectTransform;   // автоподхват
    public BoardManager board;            // автоподхват

    private Transform originParent;
    private Vector2 originAnchoredPos;
    private SlotView currentSlot;

    Coroutine placeRoutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (board == null) board = FindFirstObjectByType<BoardManager>();

        // чтобы точно ловился drag
        canvasGroup.blocksRaycasts = true;
    }

    void SaveOrigin()
    {
        originParent = transform.parent;
        originAnchoredPos = rectTransform.anchoredPosition;
    }

    public void SetInSlot(SlotView slot)
    {
        currentSlot = slot;
        SaveOrigin();
    }

    public void SetParentToSlot(RectTransform slotContentRoot)
    {
        transform.SetParent(slotContentRoot, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(originParent, false);
        rectTransform.anchoredPosition = originAnchoredPos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null || board == null)
        {
            Debug.LogError("DraggableItem: canvas или board не найден. Проверь, что есть Canvas и BoardManager в сцене.");
            return;
        }

        board.RegisterDragging(this);

        if (currentSlot != null)
        {
            currentSlot.ClearItemIfMatches(this);
            currentSlot = null;
        }

        SaveOrigin();

        transform.SetParent(canvas.transform, true);

        // важно: иначе слот не получит OnDrop
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.9f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (board != null) board.UnregisterDragging(this);

        // если дроп не случился — всё ещё в canvas => вернуть
        if (canvas != null && transform.parent == canvas.transform)
            ReturnToOrigin();

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
    public void PlayPlaceBounce()
    {
        if (placeRoutine != null) StopCoroutine(placeRoutine);
        placeRoutine = StartCoroutine(PlaceBounceRoutine());
    }

    IEnumerator PlaceBounceRoutine()
    {
        var rt = transform as RectTransform;
        if (rt == null) yield break;

        Vector2 basePos = rt.anchoredPosition;
        Vector3 baseScale = rt.localScale;

        float t;

        // аккуратные параметры
        float upY = 8f;          // было 18
        float upDur = 0.10f;     // коротко
        float returnDur = 0.14f; // мягкий возврат
        float scaleUp = 1.03f;   // было 1.06

        // 🔼 лёгкий подъём
        t = 0f;
        while (t < upDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / upDur);
            float ease = k * k * (3f - 2f * k); // SmoothStep

            rt.anchoredPosition = basePos + Vector2.up * Mathf.Lerp(0f, upY, ease);
            rt.localScale = baseScale * Mathf.Lerp(1f, scaleUp, ease);
            yield return null;
        }

        // 🔽 плавно обратно, без "переразгиба"
        t = 0f;
        while (t < returnDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / returnDur);
            float ease = 1f - Mathf.Pow(1f - k, 3f); // easeOut

            rt.anchoredPosition = basePos + Vector2.up * Mathf.Lerp(upY, 0f, ease);
            rt.localScale = baseScale * Mathf.Lerp(scaleUp, 1f, ease);
            yield return null;
        }

        rt.anchoredPosition = basePos;
        rt.localScale = baseScale;
        placeRoutine = null;
    }
}
