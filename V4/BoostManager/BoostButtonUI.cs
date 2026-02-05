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

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
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

        // тратим 1 и применяем
        if (BoostInventory.TryConsume(type, 1))
        {
            Refresh();
            BuffManager.Instance?.UseBoost(type);
        }
    }
}
