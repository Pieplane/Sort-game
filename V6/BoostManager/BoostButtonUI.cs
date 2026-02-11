using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoostButtonUI : MonoBehaviour
{
    [Header("Config")]
    public BoostType type = BoostType.ClearSomeOfType;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject plusIcon;     // зелёный плюс (Image внутри)
    [SerializeField] private GameObject countBadge;   // контейнер бейджа
    [SerializeField] private TMP_Text countText;

    [Header("Refs")]
    [SerializeField] private BoostShopPanel shopPanel; // панель покупки (в Canvas)

    [Header("Shake")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 18f;
    [SerializeField] private int shakeVibrato = 14;

    Tween shakeTween;
    Coroutine bindRoutine;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);

        if (shakeTarget == null && button != null)
            shakeTarget = button.GetComponent<RectTransform>();
    }


    public void Refresh()
    {
        // ✅ режим бесконечных бустеров
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.InfiniteBoosters)
        {
            if (plusIcon != null) plusIcon.SetActive(false);
            if (countBadge != null) countBadge.SetActive(false);
            if (countText != null) countText.text = ""; // или "∞" если хочешь

            return;
        }

        int count = BoostInventory.Get(type);

        bool has = count > 0;
        if (plusIcon != null) plusIcon.SetActive(!has);
        if (countBadge != null) countBadge.SetActive(has);
        if (countText != null) countText.text = count.ToString();
    }
    void OnEnable()
    {
        BoostInventory.OnBoostChanged += OnBoostChanged;

        // гарантированно дождёмся PlayerProgress и его загрузки
        bindRoutine = StartCoroutine(BindToProgressRoutine());

        Refresh(); // ок, но это может быть "временно" до загрузки
    }

    void OnDisable()
    {
        BoostInventory.OnBoostChanged -= OnBoostChanged;

        if (bindRoutine != null) StopCoroutine(bindRoutine);
        UnbindFromProgress();
    }
    IEnumerator BindToProgressRoutine()
    {
        // ждём пока появится инстанс
        while (PlayerProgress.Instance == null)
            yield return null;

        // подписываемся на изменения infinite
        PlayerProgress.Instance.OnInfiniteBoostersChanged += OnInfiniteChanged;

        // если прогресс ещё не загружен — ждём событие загрузки
        if (!PlayerProgress.Instance.IsLoaded)
        {
            PlayerProgress.Instance.OnLoaded += OnLoaded;
            yield break;
        }

        // уже загружено — можно обновить UI
        Refresh();
    }
    void UnbindFromProgress()
    {
        if (PlayerProgress.Instance == null) return;

        PlayerProgress.Instance.OnInfiniteBoostersChanged -= OnInfiniteChanged;
        PlayerProgress.Instance.OnLoaded -= OnLoaded;
    }
    void OnLoaded()
    {
        // данные пришли из облака — обновляем UI
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLoaded -= OnLoaded;

        Refresh();
    }
    void OnInfiniteChanged()
    {
        Refresh();
    }
    void OnBoostChanged(BoostType changedType, int newValue)
    {
        if (changedType != type) return;
        Refresh();
    }
    void OnClick()
    {
        bool infinite = PlayerProgress.Instance != null && PlayerProgress.Instance.InfiniteBoosters;

        // если не infinite — и буста нет — открываем покупку
        if (!infinite)
        {
            int count = BoostInventory.Get(type);
            if (count <= 0)
            {
                if (shopPanel != null)
                    shopPanel.OpenFor(type, onChanged: Refresh);
                AudioManager.Instance?.Play("Click");
                return;
            }
        }

        // применяем буст (внутри BuffManager решается, тратить или нет)
        bool applied = BuffManager.Instance != null && BuffManager.Instance.UseBoost(type);

        if (applied)
        {
            Refresh();
            AudioManager.Instance?.Play("Collect");
        }
        else
        {
            AudioManager.Instance?.Play("Knock");
            Shake();
        }
    }
    void Shake()
    {
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
