using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockByLevelGateUpdate : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private int requiredLevel = 8;

    [Header("UI")]
    [SerializeField] private Button lockButton;
    [SerializeField] private Button buffButton;
    [SerializeField] private TMP_Text hintText;

    [Header("Shake")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 18f;
    [SerializeField] private int shakeVibrato = 14;

    [Header("Unlock FX")]
    [SerializeField] private StarFxPool starFxPool;   // твой пул
    [SerializeField] private Canvas canvas;           // обычно главный Canvas сцены
    //[SerializeField] private int starsOnUnlock = 18;
    [SerializeField] private float popScale = 1.12f;

    Tween shakeTween;
    bool subscribed;
    Coroutine initRoutine;

    bool wasUnlocked = false; // чтобы FX проигрывался 1 раз

    void Awake()
    {
        if (lockButton != null)
            lockButton.onClick.AddListener(OnClickLocked);

        if (shakeTarget == null && lockButton != null)
            shakeTarget = lockButton.GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        SetLockedVisual();
    }

    void OnEnable()
    {
        if (initRoutine == null)
            initRoutine = StartCoroutine(InitWhenProgressReady());
    }

    void OnDisable()
    {
        if (initRoutine != null)
        {
            StopCoroutine(initRoutine);
            initRoutine = null;
        }

        if (subscribed && PlayerProgress.Instance != null)
        {
            PlayerProgress.Instance.OnLevelUp -= OnLevelUp;
            subscribed = false;
        }

        shakeTween?.Kill();
        if (shakeTarget != null) shakeTarget.DOKill();
    }

    IEnumerator InitWhenProgressReady()
    {
        while (PlayerProgress.Instance == null)
            yield return null;

        if (!subscribed)
        {
            PlayerProgress.Instance.OnLevelUp += OnLevelUp;
            subscribed = true;
        }

        // важно: инициализируем wasUnlocked корректно
        wasUnlocked = PlayerProgress.Instance.Level >= requiredLevel;
        ApplyState(playUnlockFx: false);

        initRoutine = null;
    }

    void OnLevelUp(int lvl)
    {
        // если именно на этом апе разблокировали — проиграем FX
        bool nowUnlocked = lvl >= requiredLevel;
        bool justUnlocked = !wasUnlocked && nowUnlocked;

        ApplyState(playUnlockFx: justUnlocked);
        wasUnlocked = nowUnlocked;
    }

    void ApplyState(bool playUnlockFx)
    {
        var pp = PlayerProgress.Instance;
        if (pp == null)
        {
            SetLockedVisual();
            return;
        }

        bool unlocked = pp.Level >= requiredLevel;

        if (lockButton != null) lockButton.gameObject.SetActive(!unlocked);
        if (buffButton != null) buffButton.gameObject.SetActive(unlocked);

        if (hintText != null)
            hintText.text = unlocked ? "" : $"Ранг {requiredLevel}";

        if (unlocked && playUnlockFx)
            PlayUnlockFx();
    }

    void SetLockedVisual()
    {
        if (lockButton != null) lockButton.gameObject.SetActive(true);
        if (buffButton != null) buffButton.gameObject.SetActive(false);

        if (hintText != null)
            hintText.text = $"Ранг {requiredLevel}";
    }

    void OnClickLocked()
    {
        var pp = PlayerProgress.Instance;
        if (pp != null && pp.Level >= requiredLevel)
        {
            ApplyState(playUnlockFx: true);
            return;
        }

        if (shakeTarget == null) return;

        shakeTween?.Kill();
        shakeTarget.DOKill();

        shakeTween = shakeTarget
            .DOShakeAnchorPos(
                shakeDuration,
                new Vector2(shakeStrength, 0f),
                shakeVibrato,
                randomness: 0f,
                snapping: false,
                fadeOut: true
            )
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    void PlayUnlockFx()
    {
        // 1) POP у кнопки баффа
        if (buffButton != null)
        {
            var rt = buffButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.DOKill();
                rt.localScale = Vector3.one * 0.9f;

                rt.DOScale(popScale, 0.18f).SetEase(Ease.OutBack)
                  .OnComplete(() =>
                      rt.DOScale(1f, 0.10f).SetEase(Ease.OutQuad)
                  )
                  .SetLink(buffButton.gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        // 2) Звёзды в позиции кнопки (лучше от новой кнопки, если есть)
        if (starFxPool != null && canvas != null)
        {
            Transform target = buffButton != null ? buffButton.transform : (lockButton != null ? lockButton.transform : null);
            if (target != null)
                starFxPool.PlayBurstFromWorld(target.position, canvas, StarBurstPreset.Unlock);
        }
    }
}
