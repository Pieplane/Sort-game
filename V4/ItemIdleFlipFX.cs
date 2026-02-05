using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemIdleFlipFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ItemView itemView;   // чтобы знать тип/статус
    [SerializeField] private DraggableItem drag;  // чтобы не флипать во врем€ перетаскивани€
    [SerializeField] private RectTransform visualRoot; // обычно это Image rectTransform

    [Header("Tuning")]
    [SerializeField] private float startDelayMin = 0.8f;
    [SerializeField] private float startDelayMax = 2.2f;

    [SerializeField] private float intervalMin = 3.0f;
    [SerializeField] private float intervalMax = 8.0f;

    [SerializeField] private float flipHoldMin = 0.06f;
    [SerializeField] private float flipHoldMax = 0.18f;

    [SerializeField] private float chancePerTick = 0.35f; // шанс флипа при "тикe"

    [Header("Only main items (not back items)")]
    [SerializeField] private bool disableOnNameBackItem = true;

    float baseX = 1f;
    bool flipped;

    void Awake()
    {
        if (itemView == null) itemView = GetComponent<ItemView>();
        if (drag == null) drag = GetComponent<DraggableItem>();

        // если не указали visualRoot Ч попробуем найти Image
        if (visualRoot == null)
        {
            var img = GetComponentInChildren<Image>(true);
            if (img != null) visualRoot = img.rectTransform;
            else visualRoot = transform as RectTransform;
        }

        baseX = visualRoot.localScale.x;
    }

    void OnEnable()
    {
        // если это BackItem Ч отключаем эффект
        if (disableOnNameBackItem && gameObject.name.Contains("BackItem"))
            return;

        CancelInvoke();
        Invoke(nameof(Tick), Random.Range(startDelayMin, startDelayMax));
    }

    void OnDisable()
    {
        CancelInvoke();
        Restore();
    }

    void Tick()
    {
        // планируем следующий тик сразу, чтобы не зависнуть
        Invoke(nameof(Tick), Random.Range(intervalMin, intervalMax));

        if (itemView != null && itemView.IsClearing) return;

        // если тащим Ч не делаем
        if (drag != null && drag.canvasGroup != null && drag.canvasGroup.blocksRaycasts == false)
            return;

        // шанс, чтобы не все предметы делали одновременно
        if (Random.value > chancePerTick) return;

        // уже флипнут Ч не трогаем
        if (flipped) return;

        DoFlipOnce();
    }

    void DoFlipOnce()
    {
        flipped = true;

        var s = visualRoot.localScale;
        s.x = -Mathf.Abs(baseX);
        visualRoot.localScale = s;

        Invoke(nameof(Restore), Random.Range(flipHoldMin, flipHoldMax));
    }

    void Restore()
    {
        if (visualRoot == null) return;

        var s = visualRoot.localScale;
        s.x = Mathf.Abs(baseX);
        visualRoot.localScale = s;

        flipped = false;
    }
}
