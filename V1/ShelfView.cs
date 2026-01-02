using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfView : MonoBehaviour
{
    public List<SlotView> slots = new List<SlotView>(3);

    void Awake()
    {
        slots.Clear();
        // Слоты берем в порядке по иерархии (Slot, Slot(1), Slot(2))
        // Важно: внутри полки должно быть ровно 3 SlotView.
        var found = GetComponentsInChildren<SlotView>(true);
        foreach (var s in found)
        {
            if (s.transform.parent == transform) // только прямые дети полки
                slots.Add(s);
        }

        // сортируем по сиблингу, чтобы был порядок слева-направо
        slots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
    }

    public bool TryClearTriple()
    {
        if (slots.Count != 3) return false;

        var i0 = slots[0].GetItemView();
        var i1 = slots[1].GetItemView();
        var i2 = slots[2].GetItemView();

        if (i0 == null || i1 == null || i2 == null) return false;

        if (i0.Type == i1.Type && i1.Type == i2.Type)
        {
            // удалить 3 предмета
            Destroy(i0.gameObject);
            Destroy(i1.gameObject);
            Destroy(i2.gameObject);
            return true;
        }

        return false;
    }
}
