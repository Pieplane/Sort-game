using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StarBurstPreset
{
    Default,
    Unlock,
    Big
}
public class StarFxPool : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StarFx prefab;
    [SerializeField] private RectTransform fxRoot;

    [Header("Presets")]
    [SerializeField] private StarBurstSettings defaultFx;
    [SerializeField] private StarBurstSettings unlockFx;
    [SerializeField] private StarBurstSettings bigFx;

    [Header("Pool")]
    [SerializeField] private int prewarm = 60;
    [SerializeField] private int maxCount = 120;

    private readonly List<StarFx> pool = new();
    private int rrIndex;

    public RectTransform FxRoot => fxRoot;

    void Awake()
    {
        if (fxRoot == null) fxRoot = (RectTransform)transform;
        Ensure(prewarm);
    }

    void Ensure(int target)
    {
        target = Mathf.Clamp(target, 0, maxCount);
        while (pool.Count < target)
            CreateOne();
    }

    StarFx CreateOne()
    {
        var fx = Instantiate(prefab, fxRoot);
        fx.gameObject.SetActive(false);
        pool.Add(fx);
        return fx;
    }

    StarFx Get()
    {
        for (int i = 0; i < pool.Count; i++)
            if (!pool[i].gameObject.activeSelf)
                return pool[i];

        rrIndex = (rrIndex + 1) % pool.Count;
        return pool[rrIndex];
    }

    public void PlayBurst(Vector2 pos, StarBurstPreset preset = StarBurstPreset.Default)
    {
        StarBurstSettings fx = preset switch
        {
            StarBurstPreset.Unlock => unlockFx,
            StarBurstPreset.Big => bigFx,
            _ => defaultFx
        };

        Ensure(Mathf.Max(pool.Count, fx.count));

        for (int i = 0; i < fx.count; i++)
        {
            var star = Get();
            if (star == null) return;

            star.gameObject.SetActive(true);
            star.Play(pos, fx);
        }
    }
    public void PlayBurstFromWorld(Vector3 worldPos, Canvas canvas, StarBurstPreset preset = StarBurstPreset.Default)
    {
        if (canvas == null) { Debug.LogWarning("[StarFxPool] canvas null"); return; }

        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        //Debug.Log($"[StarFxPool] canvas={canvas.name} mode={canvas.renderMode} cam={(cam ? cam.name : "NULL")} worldPos={worldPos}");

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        //Debug.Log($"[StarFxPool] screenPos={screenPos} (Screen={Screen.width}x{Screen.height})");

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fxRoot, screenPos, cam, out Vector2 localPos))
        {
            Debug.LogWarning("[StarFxPool] ScreenPointToLocalPointInRectangle FAILED");
            return;
        }

        //Debug.Log($"[StarFxPool] localPos={localPos} fxRoot={fxRoot.name} fxRootRect={fxRoot.rect}");

        PlayBurst(localPos, preset);
    }
    public void PlayBurstFromWorldWithCameras(
    Vector3 worldPos,
    Camera worldCamera,   // камера, которая видит комнату (Main/RoomCamera)
    Canvas fxCanvas,      // Canvas, где живут звезды (fxRoot находится внутри него)
    StarBurstPreset preset = StarBurstPreset.Default)
    {
        if (fxCanvas == null || fxRoot == null) return;

        // 1) World -> Screen (камерой комнаты)
        var wc = worldCamera != null ? worldCamera : Camera.main;
        if (wc == null) return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(wc, worldPos);

        // 2) Screen -> Local (камерой FX Canvas)
        var fxCam = fxCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : fxCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fxRoot, screenPos, fxCam, out Vector2 localPos))
            return;

        PlayBurst(localPos, preset);
    }
}
