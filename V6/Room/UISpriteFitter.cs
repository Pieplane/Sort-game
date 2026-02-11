using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpriteFitter : MonoBehaviour
{
    // Подгоняет Image под рамку frameSize, сохраняя пропорции
    public static void Fit(Image img, Vector2 frameSize)
    {
        if (img == null || img.sprite == null) return;

        var rt = img.rectTransform;

        // размеры спрайта в пикселях
        float w = img.sprite.rect.width;
        float h = img.sprite.rect.height;

        if (w <= 0 || h <= 0) return;

        float k = Mathf.Min(frameSize.x / w, frameSize.y / h);

        rt.sizeDelta = new Vector2(w * k, h * k);
        rt.anchoredPosition = Vector2.zero;   // центрируем
        rt.localScale = Vector3.one;
    }
}
