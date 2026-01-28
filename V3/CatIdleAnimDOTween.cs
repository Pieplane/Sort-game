using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CatIdleAnimDOTween : MonoBehaviour
{
    [Header("Position shake")]
    [SerializeField] private float moveAmount = 4f;      // пиксели
    [SerializeField] private float moveDuration = 0.06f; // скорость

    [Header("Rotation shake")]
    [SerializeField] private float rotateAngle = 2f;     // градусы
    [SerializeField] private float rotateDuration = 0.12f;

    RectTransform rect;
    Vector2 basePos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        rect.anchoredPosition = basePos;
        rect.localRotation = Quaternion.identity;

        StartPositionShake();
        StartRotationShake();
    }

    void OnDisable()
    {
        DOTween.Kill(rect);
        rect.anchoredPosition = basePos;
        rect.localRotation = Quaternion.identity;
    }

    void StartPositionShake()
    {
        rect.DOAnchorPosX(basePos.x + moveAmount, moveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void StartRotationShake()
    {
        rect.DOLocalRotate(new Vector3(0, 0, rotateAngle), rotateDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
