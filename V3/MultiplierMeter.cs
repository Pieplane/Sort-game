#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplierMeter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform needle;      // сама стрелка (каретка)
    [SerializeField] private RectTransform arcCenter;   // пустой RectTransform в центре дуги

    [Header("Arc (degrees)")]
    [SerializeField] private float minAngle = 200f;     // левый край дуги
    [SerializeField] private float maxAngle = 340f;     // правый край дуги

    [Header("Radius (pixels)")]
    [SerializeField] private float radius = 260f;       // радиус дуги в пиксел€х

    [Header("Movement")]
    [SerializeField] private float speed = 1.2f;        // скорость (сколько "t" в секунду)
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Optional rotation")]
    [SerializeField] private bool rotateToFaceCenter = true;
    [SerializeField] private float rotationOffset = 0f; // подстройка (например -90)

    [Header("Multipliers left->right")]
    [SerializeField] private int[] multipliers = { 2, 3, 5, 3, 2 };

    [Header("Debug draw")]
    //[SerializeField] private bool drawDebug = true;
    //[SerializeField] private bool drawOnlyWhenSelected = true;
    //[SerializeField] private int arcSteps = 40; // гладкость дуги

    [Header("Custom segment sizes (same length as multipliers)")]
    //[SerializeField] private float[] segmentWeights = { 15, 20, 30, 20, 15 };

    [SerializeField] private float[] segmentWeights = { 1, 1, 1, 1, 1 }; // можно мен€ть
    //[SerializeField] private bool gizmoFlipAngle = false; // если вдруг надо отзеркалить


    public bool IsRunning { get; private set; } = true;

    private float t; // 0..1
    private bool forward = true;

    public event Action<int> OnStopped;

    void Reset()
    {
        needle = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!IsRunning || needle == null || arcCenter == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        t += (forward ? 1f : -1f) * speed * dt;
        if (t >= 1f) { t = 1f; forward = false; }
        if (t <= 0f) { t = 0f; forward = true; }

        ApplyPosition(t);
    }

    void ApplyPosition(float normalized)
    {
        float angle = Mathf.Lerp(minAngle, maxAngle, normalized) * Mathf.Deg2Rad;

        // позици€ на окружности (в локальных координатах arcCenter)
        Vector2 localPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        // ставим needle так, чтобы она была дочерним объектом arcCenter (или используем InverseTransformPoint)
        // —амый простой вариант: сделай needle дочерним arcCenter в иерархии.
        needle.anchoredPosition = localPos;

        if (rotateToFaceCenter)
        {
            // повернуть так, чтобы "смотрела" в центр
            Vector2 dirToCenter = -localPos.normalized;
            float rotZ = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg + rotationOffset;
            needle.localEulerAngles = new Vector3(0, 0, rotZ);
        }
    }

    public int StopAndGetMultiplier()
    {
        if (!IsRunning) return GetMultiplierByT(t);

        IsRunning = false;
        int m = GetMultiplierByT(t);
        OnStopped?.Invoke(m);
        return m;
    }

    //public void StartRun()
    //{
    //    IsRunning = true;
    //}

    int GetMultiplierByT(float normalized)
    {
        if (multipliers == null || multipliers.Length == 0) return 1;

        int n = multipliers.Length;

        // если веса не заданы или не совпадают Ч fallback на равные сегменты
        if (segmentWeights == null || segmentWeights.Length != n)
        {
            int idxEq = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(normalized) * n), 0, n - 1);
            return multipliers[idxEq];
        }

        float sum = 0f;
        for (int i = 0; i < n; i++) sum += Mathf.Max(0f, segmentWeights[i]);
        if (sum <= 0.0001f) return multipliers[0];

        float x = Mathf.Clamp01(normalized) * sum;

        float acc = 0f;
        for (int i = 0; i < n; i++)
        {
            acc += Mathf.Max(0f, segmentWeights[i]);
            if (x <= acc) return multipliers[i];
        }

        return multipliers[n - 1];
    }
    public void StartRun()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void ResetAndStart(bool startFromLeft = true)
    {
        // сброс позиции стрелки
        t = startFromLeft ? 0f : 1f;
        forward = startFromLeft;

        ApplyPosition(t);
        IsRunning = true;
    }

    public void SetNormalized(float normalized, bool keepRunning = false)
    {
        t = Mathf.Clamp01(normalized);
        ApplyPosition(t);
        IsRunning = keepRunning;
    }
    //#if UNITY_EDITOR


    //    void OnDrawGizmos()
    //    {
    //        if (arcCenter == null || multipliers == null || multipliers.Length == 0) return;

    //        int n = multipliers.Length;

    //        // веса опциональны: если не заданы/не совпали Ч рисуем равные сегменты
    //        bool useWeights = (segmentWeights != null && segmentWeights.Length == n);

    //        float totalW = 0f;
    //        if (useWeights)
    //        {
    //            for (int i = 0; i < n; i++) totalW += Mathf.Max(0f, segmentWeights[i]);
    //            if (totalW <= 0f) useWeights = false;
    //        }

    //        // 1) дуга
    //        Gizmos.color = Color.gray;
    //        DrawArcLocal(minAngle, maxAngle, radius, 50);

    //        // 2) границы сегментов
    //        float acc = 0f;
    //        for (int i = 0; i <= n; i++)
    //        {
    //            float t = useWeights ? (acc / totalW) : (i / (float)n);
    //            Vector3 p = PointOnArcLocal(t);

    //            Gizmos.color = (i == 0 || i == n) ? Color.white : Color.yellow;
    //            Gizmos.DrawLine(arcCenter.position, p);

    //            if (i < n && useWeights)
    //                acc += Mathf.Max(0f, segmentWeights[i]);
    //        }
    //    }

    //    Vector3 PointOnArcLocal(float normalized)
    //    {
    //        float deg = Mathf.Lerp(minAngle, maxAngle, normalized);
    //        if (gizmoFlipAngle) deg = -deg;

    //        float rad = deg * Mathf.Deg2Rad;

    //        // ¬ј∆Ќќ: как у теб€ в ApplyPosition Ч cos/sin * radius, но потом в мир через TransformPoint
    //        Vector2 localPos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    //        return arcCenter.TransformPoint(localPos);
    //    }

    //    void DrawArcLocal(float minDeg, float maxDeg, float r, int steps)
    //    {
    //        Vector3 prev = PointOnArcLocal(0f);
    //        for (int i = 1; i <= steps; i++)
    //        {
    //            float t = i / (float)steps;
    //            Vector3 p = PointOnArcLocal(t);
    //            Gizmos.DrawLine(prev, p);
    //            prev = p;
    //        }
    //    }
    //#endif
}
