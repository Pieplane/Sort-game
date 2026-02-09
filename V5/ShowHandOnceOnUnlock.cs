using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowHandOnceOnUnlock : MonoBehaviour
{
    [SerializeField] private int requiredLevel = 8;
    [SerializeField] private Button targetButton;
    [SerializeField] private HandHintUI handHint;

    // уникальный ключ (лучше руками задать id)
    [SerializeField] private string uniqueId = "menu_special_button";

    string Key => $"HAND_HINT_SHOWN_{uniqueId}";

    void Awake()
    {
        if (targetButton == null) targetButton = GetComponent<Button>();
        if (targetButton != null) targetButton.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        StartCoroutine(TryShowWhenReady());
    }

    IEnumerator TryShowWhenReady()
    {
        while (PlayerProgress.Instance == null)
            yield return null;

        TryShow();
    }

    void TryShow()
    {
        if (handHint == null || targetButton == null) return;

        // уже показывали -> никогда больше
        if (PlayerPrefs.GetInt(Key, 0) == 1) return;

        // ещё закрыто
        if (PlayerProgress.Instance.Level < requiredLevel) return;

        // кнопка должна быть реально доступна
        if (!targetButton.gameObject.activeInHierarchy || !targetButton.interactable) return;

        // ставим флаг СРАЗУ, чтобы не повторялось ни при каких перезагрузках/багах
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();

        handHint.ShowFor(targetButton.GetComponent<RectTransform>());
    }

    void OnClicked()
    {
        // просто спрячем, а флаг уже стоит
        if (handHint != null) handHint.Hide();
    }
}
