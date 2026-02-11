using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomUpgradeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RoomItemDefinition itemDef;
    [SerializeField] private RoomUpgradePanel panel;

    [Header("New badge")]
    [SerializeField] private GameObject exclamation;
    [SerializeField] private bool requireEnoughCoins = false;

    private PlayerProgress _pp;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);

        if (exclamation != null) exclamation.SetActive(false);
    }

    void OnEnable()
    {
        RefreshBadge();
        RefreshVisibility();

        RoomProgress.OnRoomChanged += OnRoomChanged;

        _pp = PlayerProgress.Instance;
        if (_pp != null)
            _pp.OnLevelUp += OnLevelUp;
    }

    void OnDisable()
    {
        RoomProgress.OnRoomChanged -= OnRoomChanged;

        if (_pp != null)
            _pp.OnLevelUp -= OnLevelUp;

        _pp = null;
    }
    void OnDestroy()
    {
        // ✅ на всякий случай (иногда порядок OnDisable/OnDestroy/смена сцен сюрпризит)
        RoomProgress.OnRoomChanged -= OnRoomChanged;

        if (_pp != null)
            _pp.OnLevelUp -= OnLevelUp;

        _pp = null;
    }

    void OnLevelUp(int lvl)
    {
        if (!this) return; // ✅ защита от редких гонок
        RefreshBadge();
        RefreshVisibility();
    }

    void OnRoomChanged()
    {
        if (!this) return;
        RefreshBadge();
        RefreshVisibility();
    }

    void OnClick()
    {
        if (panel == null || itemDef == null) return;

        AudioManager.Instance?.Play("Click");

        // ✅ как только игрок открыл панель — считаем, что он "увидел" новое доступное
        MarkSeenCurrent();
        if (exclamation != null) exclamation.SetActive(false);

        panel.Open(itemDef);
    }

    void RefreshBadge()
    {
        if (exclamation == null || itemDef == null || itemDef.variants == null)
            return;

        // если прогресса ещё нет — лучше не показывать "!"
        if (PlayerProgress.Instance == null || !PlayerProgress.Instance.IsLoaded)
        {
            exclamation.SetActive(false);
            return;
        }

        int playerRank = PlayerProgress.Instance.Level;
        int coins = PlayerProgress.Instance.Coins;

        bool shouldShow = false;

        // показываем "!" если есть ХОТЯ БЫ ОДНА вариация:
        // - доступна по рангу
        // - не куплена
        // - хватает монет (опционально)
        // - ещё не seen
        for (int idx = 1; idx < itemDef.variants.Length; idx++) // 0 — дефолт
        {
            var v = itemDef.variants[idx];

            bool byRank = playerRank >= v.unlockLevel;
            if (!byRank) continue;

            if (RoomProgress.IsOwned(itemDef.id, idx)) continue;

            if (requireEnoughCoins && coins < v.priceCoins) continue;

            if (!RoomProgress.IsSeen(itemDef.id, idx))
            {
                shouldShow = true;
                break;
            }
        }

        exclamation.SetActive(shouldShow);
    }

    void MarkSeenCurrent()
    {
        if (itemDef == null || itemDef.variants == null) return;
        if (PlayerProgress.Instance == null || !PlayerProgress.Instance.IsLoaded) return;

        int playerRank = PlayerProgress.Instance.Level;
        int coins = PlayerProgress.Instance.Coins;

        // ✅ помечаем как seen все “новые доступные” (или можешь отметить только первую — но так честнее)
        for (int idx = 1; idx < itemDef.variants.Length; idx++)
        {
            var v = itemDef.variants[idx];

            if (playerRank < v.unlockLevel) continue;
            if (RoomProgress.IsOwned(itemDef.id, idx)) continue;
            if (requireEnoughCoins && coins < v.priceCoins) continue;

            if (!RoomProgress.IsSeen(itemDef.id, idx))
                RoomProgress.SetSeen(itemDef.id, idx, true);
        }
    }

    void RefreshVisibility()
    {
        if (itemDef == null || itemDef.variants == null || itemDef.variants.Length == 0)
        {
            gameObject.SetActive(true);
            return;
        }

        int maxIdx = itemDef.variants.Length - 1;

        bool maxOwned = RoomProgress.IsOwned(itemDef.id, maxIdx);
        int equipped = RoomProgress.GetEquipped(itemDef.id);
        bool maxEquipped = (equipped == maxIdx);

        bool shouldHide = maxOwned && maxEquipped;
        gameObject.SetActive(!shouldHide);
    }
}
