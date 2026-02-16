using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionCardUI : MonoBehaviour
{
    [SerializeField] private Image silhouette;
    [SerializeField] private Image reveal;
    [SerializeField] private TMP_Text percentText;

    public void Set(Sprite sprite, float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        // черная подложка
        silhouette.sprite = sprite;
        silhouette.color = Color.black;

        // цветной слой
        reveal.sprite = sprite;
        reveal.color = Color.white;
        reveal.fillAmount = progress01;

        //if (percentText != null)
        //{
        //    if (progress01 >= 1f)
        //        percentText.text = "";
        //    else
        //        percentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
        //}
        if (percentText != null)
        {
            if (progress01 <= 0f)
                percentText.text = "";              // ✅ 0% не показываем
            else if (progress01 >= 1f)
                percentText.text = "";              // ✅ 100% тоже можно скрыть
            else
                percentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
        }
    }
}
