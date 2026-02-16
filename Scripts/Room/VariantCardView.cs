using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VariantCardView : MonoBehaviour
{
    [SerializeField] private RectTransform previewFrame;
    [SerializeField] private Image previewImage;

    public void SetSprite(Sprite spr)
    {
        previewImage.sprite = spr;
        previewImage.preserveAspect = true;

        // בונ¸ל נואכüםûי נאחלונ נאלךט
        Vector2 frameSize = previewFrame.rect.size;

        UISpriteFitter.Fit(previewImage, frameSize);
    }
}
