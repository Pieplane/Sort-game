using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockByLevelGate : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private int requiredLevel = 8;

    [Header("UI")]
    [SerializeField] private Button lockButton;    // кнопка с замком
    [SerializeField] private Button buffButton;    // настоящая кнопка баффа
    [SerializeField] private TMP_Text hintText;    // опционально

    [Header("Shake")]
    [SerializeField] private RectTransform shakeTarget; // что трясти (если null — lockButton)
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 18f;
    [SerializeField] private int shakeVibrato = 14;

    Tween shakeTween;
    bool subscribed;
    Coroutine initRoutine;

    void Awake()
    {
        if (lockButton != null)
            lockButton.onClick.AddListener(OnClickLocked);

        if (shakeTarget == null && lockButton != null)
            shakeTarget = lockButton.GetComponent<RectTransform>();

        // На старте лучше показать замок (пока прогресс не готов),
        // чтобы не было моргания
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
        // ждём пока PlayerProgress появится
        while (PlayerProgress.Instance == null)
            yield return null;

        // подписка
        if (!subscribed)
        {
            PlayerProgress.Instance.OnLevelUp += OnLevelUp;
            subscribed = true;
        }

        ApplyState();
        initRoutine = null;
    }

    void OnLevelUp(int lvl) => ApplyState();

    void ApplyState()
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
            ApplyState();
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
}
