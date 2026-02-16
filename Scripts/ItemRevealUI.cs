using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemRevealUI : MonoBehaviour
{
    [SerializeField] private Image revealImage; // та, что Type=Filled
    [Range(0f, 1f)]
    [SerializeField] private float progress; // для отладки

    public void SetProgress(float value01)
    {
        progress = Mathf.Clamp01(value01);
        revealImage.fillAmount = progress;
    }

    // Например: +15% за каждую тройку
    public void AddStep(float step01)
    {
        SetProgress(progress + step01);
    }
}
