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
    [SerializeField] private GameObject exclamation; // объект с "!" (включаем/выключаем)
    [SerializeField] private bool requireEnoughCoins = false; // если хочешь показывать "!" только когда хватает монет

    string SeenKey => $"room_upgrade_seen_unlock_{itemDef.id}";

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

        // чтобы "!" появлялся, когда игрок апается
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLevelUp += OnLevelUp;
    }

    void OnDisable()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLevelUp -= OnLevelUp;
    }
    void OnLevelUp(int lvl) => RefreshBadge();

    void OnClick()
    {
        if (panel == null || itemDef == null) return;

        // ✅ как только игрок открыл панель — считаем, что он "увидел" новинку
        MarkSeenCurrent();
        if (exclamation != null) exclamation.SetActive(false);

        panel.Open(itemDef);
    }
    void RefreshBadge()
    {
        if (exclamation == null || itemDef == null || itemDef.variants == null) return;
        if (PlayerProgress.Instance == null) { exclamation.SetActive(false); return; }

        int playerLevel = PlayerProgress.Instance.Level;
        int seenUnlock = PlayerPrefs.GetInt(SeenKey, 0);

        // ищем “самую новую” доступную, но ещё не купленную вариацию
        int bestAvailableUnlock = 0;

        for (int idx = 1; idx < itemDef.variants.Length; idx++) // idx=0 дефолт пропускаем
        {
            var v = itemDef.variants[idx];

            bool byLevel = playerLevel >= v.unlockLevel;
            if (!byLevel) continue;

            bool owned = RoomProgress.IsOwned(itemDef.id, idx);
            if (owned) continue;

            if (requireEnoughCoins && PlayerProgress.Instance.Coins < v.priceCoins)
                continue;

            // доступно к покупке/просмотру
            if (v.unlockLevel > bestAvailableUnlock)
                bestAvailableUnlock = v.unlockLevel;
        }

        bool shouldShow = bestAvailableUnlock > 0 && bestAvailableUnlock > seenUnlock;
        exclamation.SetActive(shouldShow);
    }
    void MarkSeenCurrent()
    {
        if (itemDef == null || itemDef.variants == null) return;
        if (PlayerProgress.Instance == null) return;

        int playerLevel = PlayerProgress.Instance.Level;

        int bestAvailableUnlock = 0;
        for (int idx = 1; idx < itemDef.variants.Length; idx++)
        {
            var v = itemDef.variants[idx];

            if (playerLevel < v.unlockLevel) continue;

            bool owned = RoomProgress.IsOwned(itemDef.id, idx);
            if (owned) continue;

            if (requireEnoughCoins && PlayerProgress.Instance.Coins < v.priceCoins)
                continue;

            if (v.unlockLevel > bestAvailableUnlock)
                bestAvailableUnlock = v.unlockLevel;
        }

        // если сейчас реально есть что “новое” — записываем, что игрок это увидел
        if (bestAvailableUnlock > 0)
        {
            int prev = PlayerPrefs.GetInt(SeenKey, 0);
            if (bestAvailableUnlock > prev)
                PlayerPrefs.SetInt(SeenKey, bestAvailableUnlock);
        }
    }
}
