using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoinFlyDOTween : MonoBehaviour
{
    public static CoinFlyDOTween Instance;

    [Header("Refs (optional manual)")]
    [SerializeField] private RectTransform canvasRoot; // можно не назначать, найдём
    [SerializeField] private RectTransform coinTarget; // можно не назначать, найдём
    [SerializeField] private Image coinPrefab;

    [Header("Settings")]
    public int coins = 12;
    public float spawnRadius = 120f;
    public float flyTime = 0.6f;

    public System.Action OnCoinArrived;

    [Header("Debug")]
    public bool debugLog = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnDisable()
    {
        DOTween.Kill(gameObject);   // убьёт все твины, залинкованные на этот GO
        transform.DOKill();         // на всякий
        GetComponent<CanvasGroup>()?.DOKill();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // после каждой загрузки сцены перепривязываемся
        Rebind();
    }

    public void Rebind()
    {
        // CanvasRoot
        if (canvasRoot == null || canvasRoot.Equals(null))
        {
            // 1) пробуем найти тегнутый root
            var rootTag = FindFirstObjectByType<CanvasRootTag>();
            if (rootTag != null) canvasRoot = rootTag.GetComponent<RectTransform>();

            // 2) иначе берём первый Canvas
            if (canvasRoot == null || canvasRoot.Equals(null))
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) canvasRoot = canvas.GetComponent<RectTransform>();
            }
        }

        // Target
        if (coinTarget == null || coinTarget.Equals(null))
        {
            var targetTag = FindFirstObjectByType<CoinTargetTag>();
            if (targetTag != null) coinTarget = targetTag.GetComponent<RectTransform>();
        }

        if (debugLog)
        {
            Debug.Log($"[CoinFlyDOTween] Rebind: canvasRoot={(canvasRoot ? canvasRoot.name : "NULL")}  coinTarget={(coinTarget ? coinTarget.name : "NULL")}");
        }
    }

    public void SetTarget(RectTransform newTarget)
    {
        coinTarget = newTarget;
    }

    public void SetCanvasRoot(RectTransform newRoot)
    {
        canvasRoot = newRoot;
    }

    public void Play(Vector2 startAnchoredPos)
    {
        if (!EnsureRefs())
        {
            if (debugLog) Debug.LogWarning("[CoinFlyDOTween] Play skipped (missing refs)");
            return;
        }

        for (int i = 0; i < coins; i++)
            SpawnCoin(startAnchoredPos, i * 0.04f, i == coins - 1);
    }

    private bool EnsureRefs()
    {
        if (coinPrefab == null) return false;

        if (canvasRoot == null || canvasRoot.Equals(null) || coinTarget == null || coinTarget.Equals(null))
            Rebind();

        if (canvasRoot == null || canvasRoot.Equals(null)) return false;
        if (coinTarget == null || coinTarget.Equals(null)) return false;

        return true;
    }

    void SpawnCoin(Vector2 startPos, float delay, bool isLast)
    {
        // защита на случай, если в процессе сцена сменилась
        if (!EnsureRefs()) return;

        Image coin = Instantiate(coinPrefab, canvasRoot);
        RectTransform rt = coin.rectTransform;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        rt.anchoredPosition = startPos + offset;
        rt.localScale = Vector3.one * 0.5f;

        Vector2 targetPos = GetTargetAnchoredPos(coinTarget);

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.SetTarget(rt);
        seq.SetLink(coin.gameObject, LinkBehaviour.KillOnDestroy);

        seq.Append(rt.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        seq.Append(rt.DOAnchorPos(targetPos, flyTime).SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(0.2f, flyTime));

        seq.OnComplete(() =>
        {
            if (isLast) OnCoinArrived?.Invoke();
            if (coin != null) Destroy(coin.gameObject);
        });
    }

    Vector2 GetTargetAnchoredPos(RectTransform target)
    {
        // target может стать missing прямо здесь при смене сцены
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
