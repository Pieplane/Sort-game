using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotView : MonoBehaviour, IDropHandler
{
    public RectTransform contentRoot;
    [HideInInspector] public ShelfView shelf; // назначит ShelfView в Awake
    private BoardManager board;

    void Awake()
    {
        if (contentRoot == null)
        {
            var t = transform.Find("ContentRoot");
            if (t != null) contentRoot = (RectTransform)t;
        }

        board = FindFirstObjectByType<BoardManager>();
        shelf = GetComponentInParent<ShelfView>();
    }

    // ✅ Свойство, а не метод
    public bool HasItem
    {
        get
        {
            if (contentRoot == null) return false;

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                if (contentRoot.GetChild(i).GetComponent<DraggableItem>() != null)
                    return true;
            }
            return false;
        }
    }
    public ItemView GetItemView()
    {
        if (contentRoot == null) return null;
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            var iv = contentRoot.GetChild(i).GetComponent<ItemView>();
            if (iv != null) return iv;
        }
        return null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (board == null) return;

        var go = eventData.pointerDrag;
        if (go == null) return;

        var item = go.GetComponent<DraggableItem>();
        if (item == null) return;

        // если слот уже занят — отклоняем
        if (HasItem)
        {
            item.ReturnToOrigin();
            return;
        }

        AcceptItem(item);
        board.OnItemPlaced(this); // ✅ сообщаем менеджеру: предмет положен
    }

    // ✅ public — чтобы BoardManager мог вызвать
    public void AcceptItem(DraggableItem item)
    {
        // железобетонная защита: чистим слот перед укладкой
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            if (child.GetComponent<DraggableItem>() != null)
                Destroy(child.gameObject);
        }

        item.SetParentToSlot(contentRoot);
        item.SetInSlot(this);

        item.PlayPlaceBounce(); // ✅ эффект установки
    }

    // вызывается предметом при начале drag
    public void ClearItemIfMatches(DraggableItem item)
    {
        // логика теперь основана на childCount — ничего делать не нужно
    }
}
