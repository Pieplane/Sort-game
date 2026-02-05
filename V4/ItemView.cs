using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemType
{
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V
}

[RequireComponent(typeof(Image))]
public class ItemView : MonoBehaviour
{
    public ItemType Type { get; private set; }

    // ✅ чтобы не “чистить” один и тот же предмет несколько раз
    public bool IsClearing { get; private set; }

    [Header("Auto refs")]
    [SerializeField] private Image image;

    private BoardManager board;

    void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        board = BoardManager.Instance;   // ✅ без поиска
        board?.RegisterItem(this);
    }
    private void OnDestroy() { 
        if (IsClearing)
            board?.NotifyItemClearingEnd(this); 
        board?.UnregisterItem(this); 
    }

    public void SetType(ItemType type, ItemVisualConfig visuals)
    {
        Type = type;

        if (visuals != null)
        {
            var sp = visuals.GetSprite(type);
            if (sp != null) image.sprite = sp;
        }
    }
    public void MarkClearing()
    {
        if (IsClearing) return;
        IsClearing = true;
        board?.NotifyItemClearingStart(this);
    }
    // ВАЖНО: вызвать когда очистка реально закончилась (перед Destroy)
    public void NotifyClearingFinished()
    {
        if (!IsClearing) return; 
        IsClearing = false; 
        board?.NotifyItemClearingEnd(this); //board?.UnregisterItem(this);
                                                                                         
    }

    public IEnumerator PlayClearPopCollapse(
    float popScale = 1.12f,
    float popTime = 0.08f,
    float collapseTime = 0.12f
)
    {
        // если объект уже умирает — выходим
        if (this == null) yield break;

        var rt = GetComponent<RectTransform>();
        Vector3 startScale = rt != null ? rt.localScale : transform.localScale;

        // POP
        float t = 0f;
        while (t < popTime)
        {
            if (this == null) yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popTime);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            SetScaleSafe(Vector3.Lerp(startScale, startScale * popScale, e), rt);
            yield return null;
        }

        // COLLAPSE
        t = 0f;
        while (t < collapseTime)
        {
            if (this == null) yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / collapseTime);
            float e = k * k;
            SetScaleSafe(Vector3.Lerp(startScale * popScale, Vector3.zero, e), rt);
            yield return null;
        }

        // ✅ ТОЛЬКО ЗДЕСЬ уничтожаем объект
        NotifyClearingFinished();
        Destroy(gameObject);
    }

    void SetScaleSafe(Vector3 s, RectTransform rt)
    {
        if (this == null) return;

        if (rt != null)
            rt.localScale = s;
        else
            transform.localScale = s;
    }
}
