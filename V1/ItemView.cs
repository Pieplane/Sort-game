using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemType
{
    A,
    B
}

[RequireComponent(typeof(Image))]
public class ItemView : MonoBehaviour
{
    public ItemType Type { get; private set; }

    [Header("Auto refs")]
    [SerializeField] private Image image;

    void Awake()
    {
        if (image == null) image = GetComponent<Image>();
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
}
