using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShelfView : MonoBehaviour
{
    [Header("Back preview (ghost icons)")]
    public float previewYOffset = 18f;
    public float previewScale = 0.9f;
    public float previewAlpha = 0.75f;
    public float previewGray = 0.8f;

    private Image[] previewImages = new Image[3];

    public List<SlotView> slots = new List<SlotView>(3);

    [Header("Back items (optional)")]
    public bool hasBackItems = false;

    [Tooltip("Сколько раз эта полка может получить пачку из 3 предметов (после опустошения полки).")]
    public int backPacks = 1;

    public int ActiveClears { get; private set; } = 0;

    [Header("FX")]
    [SerializeField] private StarFxPool starFxPool;

    [Header("FX Offset")]
    [SerializeField] private Vector2 starOffset = new Vector2(0f, -25f);


    void Awake()
    {
        slots.Clear();

        var found = GetComponentsInChildren<SlotView>(true);
        foreach (var s in found)
        {
            if (s.transform.parent == transform)
                slots.Add(s);
        }

        slots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
    }

    public bool TryClearTriple()
    {
        if (slots == null || slots.Count != 3) return false;

        var i0 = slots[0].GetItemView();
        var i1 = slots[1].GetItemView();
        var i2 = slots[2].GetItemView();

        if (i0 == null || i1 == null || i2 == null) return false;
        if (i0.IsClearing || i1.IsClearing || i2.IsClearing) return false;

        if (i0.Type != i1.Type || i1.Type != i2.Type) return false;

        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Collect");
        }

        // помечаем заранее (чтобы не поймать повторную чистку на следующем проходе)
        i0.MarkClearing();
        i1.MarkClearing();
        i2.MarkClearing();

        // запускаем один общий FX-процесс
        StartCoroutine(ClearTripleFx(i0, i1, i2));
        return true;
    }

    IEnumerator ClearTripleFx(ItemView a, ItemView b, ItemView c)
    {
        ActiveClears++;

        // 1 кадр можно оставить (иногда помогает избежать гонок с layout)
        yield return null;

        // STAR FX
        if (starFxPool != null)
        {
            PlayStarsAtItem(a);
            PlayStarsAtItem(b);
            PlayStarsAtItem(c);
        }

        // Запускаем клир на САМИХ айтемах (они сами себя Destroy в конце)
        if (a != null) a.StartCoroutine(a.PlayClearPopCollapse());
        if (b != null) b.StartCoroutine(b.PlayClearPopCollapse());
        if (c != null) c.StartCoroutine(c.PlayClearPopCollapse());

        // Ждём пока объекты реально исчезнут (Destroy произойдёт в конце корутин ItemView)
        //yield return new WaitUntil(() => a == null && b == null && c == null);
        // Ждём пока объекты реально исчезнут (Destroy в конце корутины ItemView)
        // + защита от зависания
        float t = 0f;
        while ((a != null || b != null || c != null) && t < 2f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ActiveClears--;
    }

    public bool IsEmpty3()
    {
        if (slots == null || slots.Count < 3) return false;
        return slots[0].GetItemView() == null
            && slots[1].GetItemView() == null
            && slots[2].GetItemView() == null;
    }

    public void PlacePack(BoardManager board, ItemVisualConfig visuals, ItemType a, ItemType b, ItemType c)
    {
        if (slots == null || slots.Count < 3) return;

        ItemType[] pack = { a, b, c };

        for (int i = 0; i < 3; i++)
        {
            var slot = slots[i];

            var item = Object.Instantiate(board.itemPrefab, slot.contentRoot);
            item.name = "BackItem";

            var iv = item.GetComponent<ItemView>();
            if (iv == null) iv = item.gameObject.AddComponent<ItemView>();

            iv.SetType(pack[i], visuals);
            item.SetInSlot(slot);

            item.PlaySlideInFromBack(); // если нет — замени/убери
        }
    }

    Image EnsurePreviewImage(int index)
    {
        if (previewImages[index] != null) return previewImages[index];

        var slot = slots[index];

        var go = new GameObject("__BackPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(slot.contentRoot, false);
        go.transform.SetAsFirstSibling();

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, previewYOffset);
        rt.localScale = Vector3.one * previewScale;
        rt.sizeDelta = new Vector2(110, 110);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;

        previewImages[index] = img;
        return img;
    }

    public void ShowBackPreview(ItemVisualConfig visuals, ItemType a, ItemType b, ItemType c)
    {
        if (slots == null || slots.Count < 3) return;
        if (visuals == null) { HideBackPreview(); return; } // ✅

        var pack = new[] { a, b, c };

        for (int i = 0; i < 3; i++)
        {
            var img = EnsurePreviewImage(i);
            img.sprite = visuals.GetSprite(pack[i]);
            img.preserveAspect = true;

            float g = previewGray;
            img.color = new Color(g, g, g, previewAlpha);

            img.enabled = true;
        }
    }

    public void HideBackPreview()
    {
        for (int i = 0; i < 3; i++)
        {
            if (previewImages[i] != null)
                previewImages[i].enabled = false;
        }
    }

    void PlayStarsAtItem(ItemView item)
    {
        if (item == null || starFxPool == null) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, item.transform.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                starFxPool.FxRoot, screenPos, cam, out Vector2 localPos))
            return;

        localPos += starOffset;

        starFxPool.PlayBurst(localPos);
    }
    public void PlayStarsForItem(ItemView item)
    {
        if (item == null) return;
        if (starFxPool == null) return;
        PlayStarsAtItem(item);
    }
    //public bool IsTripleNow()
    //{
    //    if (slots == null || slots.Count != 3) return false;

    //    var a = slots[0].GetItemView();
    //    var b = slots[1].GetItemView();
    //    var c = slots[2].GetItemView();

    //    if (a == null || b == null || c == null) return false;
    //    if (a.IsClearing || b.IsClearing || c.IsClearing) return false;

    //    return a.Type == b.Type && b.Type == c.Type;
    //}
}
