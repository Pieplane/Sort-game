using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShelfView : MonoBehaviour
{
    [Header("Back preview (ghost icons)")]
    public float previewYOffset = 18f;     // чуть выше основного
    public float previewScale = 0.9f;      // чуть меньше
    public float previewAlpha = 0.75f;     // прозрачность "затемнения"
    public float previewGray = 0.8f;       // серость (1 = без серости)

    private Image[] previewImages = new Image[3];

    public List<SlotView> slots = new List<SlotView>(3);

    [Header("Back items (optional)")]
    public bool hasBackItems = false;

    [Tooltip("Сколько раз эта полка может получить пачку из 3 предметов (после опустошения полки).")]
    public int backPacks = 1;

    public int ActiveClears { get; private set; } = 0;

    void Awake()
    {
        slots.Clear();

        // Берём SlotView только среди прямых детей полки
        var found = GetComponentsInChildren<SlotView>(true);
        foreach (var s in found)
        {
            if (s.transform.parent == transform)
                slots.Add(s);
        }

        // порядок слева-направо
        slots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
    }

    public bool TryClearTriple()
    {
        if (slots.Count != 3) return false;

        var i0 = slots[0].GetItemView();
        var i1 = slots[1].GetItemView();
        var i2 = slots[2].GetItemView();

        if (i0 == null || i1 == null || i2 == null) return false;

        // ✅ если уже чистим — не трогаем
        if (i0.IsClearing || i1.IsClearing || i2.IsClearing) return false;

        if (i0.Type == i1.Type && i1.Type == i2.Type)
        {
            // ✅ помечаем, чтобы не повторялось в следующем проходе
            i0.MarkClearing();
            i1.MarkClearing();
            i2.MarkClearing();

            // ✅ запускаем эффект
            StartCoroutine(ClearTripleFx(i0, i1, i2));
            return true;
        }

        return false;
    }
    IEnumerator ClearTripleFx(ItemView a, ItemView b, ItemView c)
    {
        ActiveClears++;
        // запускаем анимации параллельно
        yield return null; // 1 кадр, чтобы state обновился

        var ca = StartCoroutine(a.PlayClearPopCollapse());
        var cb = StartCoroutine(b.PlayClearPopCollapse());
        var cc = StartCoroutine(c.PlayClearPopCollapse());

        // ждём, пока все схлопнутся
        yield return ca;
        yield return cb;
        yield return cc;

        Destroy(a.gameObject);
        Destroy(b.gameObject);
        Destroy(c.gameObject);

        ActiveClears--;
    }

    public bool IsEmpty3()
    {
        if (slots == null || slots.Count < 3) return false;
        return slots[0].GetItemView() == null
            && slots[1].GetItemView() == null
            && slots[2].GetItemView() == null;
    }

    public void PlacePack(BoardManager board, ItemType a, ItemType b, ItemType c)
    {
        if (slots.Count < 3) return;

        ItemType[] pack = { a, b, c };

        for (int i = 0; i < 3; i++)
        {
            var slot = slots[i];

            var item = Object.Instantiate(board.itemPrefab, slot.contentRoot);
            item.name = "BackItem";

            var iv = item.GetComponent<ItemView>();
            if (iv == null) iv = item.gameObject.AddComponent<ItemView>();

            iv.SetType(pack[i], board.visuals);
            item.SetInSlot(slot);

            item.PlaySlideInFromBack(); // если нет метода — замени на PlayPlaceBounce() или убери
        }
    }
    Image EnsurePreviewImage(int index)
    {
        if (previewImages[index] != null) return previewImages[index];

        var slot = slots[index];

        // создаём под slot.contentRoot отдельную картинку-превью
        var go = new GameObject("__BackPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(slot.contentRoot, false);

        // чтобы было "за" основным предметом — ставим первым в иерархии
        go.transform.SetAsFirstSibling();

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, previewYOffset);
        rt.localScale = Vector3.one * previewScale;
        rt.sizeDelta = new Vector2(90, 90); // подгони под свой размер слота

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;

        previewImages[index] = img;
        return img;
    }

    public void ShowBackPreview(ItemVisualConfig visuals, ItemType a, ItemType b, ItemType c)
    {
        if (slots == null || slots.Count < 3) return;

        var pack = new[] { a, b, c };

        for (int i = 0; i < 3; i++)
        {
            var img = EnsurePreviewImage(i);
            img.sprite = visuals.GetSprite(pack[i]);  // см. ниже: нужен метод GetSprite
            img.preserveAspect = true;

            // "затемнение": серый + альфа
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
}
