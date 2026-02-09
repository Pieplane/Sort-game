using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCursor : MonoBehaviour
{
    [SerializeField] private RectTransform hand;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

    [Header("Cursor mode")]
    [SerializeField] private bool confineCursor = true;   // вместо lock
    [SerializeField] private bool forceHideEachFrame = true;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        ApplyCursorState(true);
    }

    void OnEnable()
    {
        ApplyCursorState(true);
    }

    void OnDisable()
    {
        ApplyCursorState(false);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyCursorState(true);
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused) ApplyCursorState(true);
    }

    void Update()
    {
        if (hand == null || canvas == null) return;

        if (forceHideEachFrame)
            ApplyCursorState(true);

        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            Input.mousePosition,
            cam,
            out var pos
        );

        hand.anchoredPosition = pos + offset;
    }

    void LateUpdate()
    {
        // чтобы рука была поверх всего UI
        if (hand != null) hand.SetAsLastSibling();
    }

    void ApplyCursorState(bool useHandCursor)
    {
        if (useHandCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = confineCursor ? CursorLockMode.Confined : CursorLockMode.None;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
