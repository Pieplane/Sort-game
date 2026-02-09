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

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);

        if (shakeTarget == null && button != null)
            shakeTarget = button.GetComponent<RectTransform>();
    }


    public void Refresh()
    {
        int count = BoostInventory.Get(type);

        bool has = count > 0;
        if (plusIcon != null) plusIcon.SetActive(!has);
        if (countBadge != null) countBadge.SetActive(has);
        if (countText != null) countText.text = count.ToString();
    }
    void OnEnable()
    {
        BoostInventory.OnBoostChanged += OnBoostChanged;
        Refresh();
    }

    void OnDisable()
    {
        BoostInventory.OnBoostChanged -= OnBoostChanged;
    }
    void OnBoostChanged(BoostType changedType, int newValue)
    {
        if (changedType != type) return;
        Refresh();
    }
    void OnClick()
    {
        
        int count = BoostInventory.Get(type);

        if (count <= 0)
        {
            // открываем покупку
            if (shopPanel != null)
                shopPanel.OpenFor(type, onChanged: Refresh);
            return;
        }
        // 1) пробуем применить
        bool applied = BuffManager.Instance != null && BuffManager.Instance.UseBoost(type);

        if (!applied)
        {
            AudioManager.Instance?.Play("Knock");
            Shake();
            return;
        }

        // 2) если применилось — тогда тратим
        if (BoostInventory.TryConsume(type, 1))
        {
            Refresh();
            AudioManager.Instance?.Play("Collect");
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
