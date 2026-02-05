using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class XpFlyDOTween : MonoBehaviour
{
    public static XpFlyDOTween Instance;

    [Header("Refs (optional manual)")]
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform xpTarget;
    [SerializeField] private Image xpPrefab;

    [Header("Settings")]
    public int pieces = 10;
    public float spawnRadius = 120f;
    public float flyTime = 0.6f;

    public System.Action OnXpArrived;

    [Header("Debug")]
    public bool debugLog = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        DOTween.Kill(gameObject);   // убьёт все твины, залинкованные на этот GO
        transform.DOKill();         // на всякий
        GetComponent<CanvasGroup>()?.DOKill();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
    }

    public void Rebind()
    {
        // CanvasRoot
        if (canvasRoot == null || canvasRoot.Equals(null))
        {
            var rootTag = FindFirstObjectByType<CanvasRootTag>();
            if (rootTag != null) canvasRoot = rootTag.GetComponent<RectTransform>();

            if (canvasRoot == null || canvasRoot.Equals(null))
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) canvasRoot = canvas.GetComponent<RectTransform>();
            }
        }

        // Target
        if (xpTarget == null || xpTarget.Equals(null))
        {
            var tag = FindFirstObjectByType<XpTargetTag>();
            if (tag != null) xpTarget = tag.GetComponent<RectTransform>();
        }

        if (debugLog)
            Debug.Log($"[XpFlyDOTween] Rebind: canvasRoot={(canvasRoot ? canvasRoot.name : "NULL")} xpTarget={(xpTarget ? xpTarget.name : "NULL")}");
    }

    public void SetTarget(RectTransform newTarget) => xpTarget = newTarget;
    public void SetCanvasRoot(RectTransform newRoot) => canvasRoot = newRoot;

    public void Play(Vector2 startAnchoredPos, float startDelay = 0f)
    {
        if (!EnsureRefs())
        {
            if (debugLog) Debug.LogWarning("[XpFlyDOTween] Play skipped (missing refs)");
            return;
        }

        for (int i = 0; i < pieces; i++)
            SpawnOne(startAnchoredPos, startDelay + i * 0.03f, i == pieces - 1);
    }

    private bool EnsureRefs()
    {
        if (xpPrefab == null) return false;

        if (canvasRoot == null || canvasRoot.Equals(null) || xpTarget == null || xpTarget.Equals(null))
            Rebind();

        if (canvasRoot == null || canvasRoot.Equals(null)) return false;
        if (xpTarget == null || xpTarget.Equals(null)) return false;

        return true;
    }

    void SpawnOne(Vector2 startPos, float delay, bool isLast)
    {
        if (!EnsureRefs()) return;

        Image img = Instantiate(xpPrefab, canvasRoot);
        RectTransform rt = img.rectTransform;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        rt.anchoredPosition = startPos + offset;
        rt.localScale = Vector3.one * 0.45f;

        Vector2 targetPos = GetTargetAnchoredPos(xpTarget);

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.SetTarget(rt);
        seq.SetLink(img.gameObject, LinkBehaviour.KillOnDestroy);

        seq.Append(rt.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
        seq.Append(rt.DOAnchorPos(targetPos, flyTime).SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(0.2f, flyTime));

        seq.OnComplete(() =>
        {
            if (isLast) OnXpArrived?.Invoke();
            if (img != null) Destroy(img.gameObject);
        });
    }

    Vector2 GetTargetAnchoredPos(RectTransform target)
    {
        if (target == null || target.Equals(null) || canvasRoot == null || canvasRoot.Equals(null))
            return Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            RectTransformUtility.WorldToScreenPoint(null, target.position),
            null,
            out Vector2 localPoint
        );
        return localPoint;
    }
}
