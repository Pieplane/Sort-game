using DG.Tweening;
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

    //Coroutine placeRoutine;

    Tween placeTween;
    Tween slideTween;

    public SlotView CurrentSlot => currentSlot;

    private Vector3 originScale;
    Tween pickTween;
    //private float yOffset = 8f;


    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (board == null) board = FindFirstObjectByType<BoardManager>();

        // чтобы точно ловился drag
        canvasGroup.blocksRaycasts = true;
    }
    void OnDestroy()
    {
        placeTween?.Kill();
        slideTween?.Kill();
    }

    void SaveOrigin()
    {
        originParent = transform.parent;
        originAnchoredPos = rectTransform.anchoredPosition;
        originScale = rectTransform.localScale;
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
        rectTransform.localScale = Vector3.one;
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(originParent, false);
        rectTransform.anchoredPosition = originAnchoredPos;
        rectTransform.localScale = originScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null || board == null)
        {
            Debug.LogError("DraggableItem: canvas или board не найден. Проверь, что есть Canvas и BoardManager в сцене.");
            return;
        }
        //if (board != null && board.inputLocked) return;

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

        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Pick");
        }

        PlayPickupPop();
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

        pickTween?.Kill();
        if (rectTransform != null)
            rectTransform.localScale = originScale;
    }
    public void PlayPlaceBounce()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;

        // убиваем прошлую анимацию
        placeTween?.Kill();

        Vector2 basePos = rt.anchoredPosition;
        Vector3 baseScale = rt.localScale;

        float upY = 8f;
        float upDur = 0.10f;
        float returnDur = 0.14f;
        float scaleUp = 1.03f;

        placeTween = DOTween.Sequence()
            .SetUpdate(true) // работает при timeScale=0
            .Append(rt.DOAnchorPos(basePos + Vector2.up * upY, upDur).SetEase(Ease.OutQuad))
            .Join(rt.DOScale(baseScale * scaleUp, upDur).SetEase(Ease.OutQuad))
            .Append(rt.DOAnchorPos(basePos, returnDur).SetEase(Ease.OutCubic))
            .Join(rt.DOScale(baseScale, returnDur).SetEase(Ease.OutCubic))
            .OnKill(() =>
            {
                // гарантируем финальные значения
                if (rt) rt.anchoredPosition = basePos;
                if (rt) rt.localScale = baseScale;
            });
    }
    public void PlaySlideInFromBack()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;

        slideTween?.Kill();

        Vector2 endPos = rt.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, 40f); // ⬅️ сверху вниз

        rt.anchoredPosition = startPos;

        slideTween = rt
            .DOAnchorPos(endPos, 0.45f)          // ⬅️ очень плавно
            .SetEase(Ease.OutSine)               // ⬅️ мягкое замедление
            .SetUpdate(true);
    }
    void PlayPickupPop()
    {
        if (rectTransform == null) return;

        pickTween?.Kill();

        // Только масштаб, без позиции — иначе будет "съезжать"
        pickTween = rectTransform.DOPunchScale(Vector3.one * 0.16f, 0.20f, 10, 0.9f)
            .SetUpdate(true);
    }
}
