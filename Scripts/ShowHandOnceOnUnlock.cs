using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowHandOnceOnUnlock : MonoBehaviour
{
    [SerializeField] private int requiredLevel = 8;
    [SerializeField] private Button targetButton;
    [SerializeField] private HandHintUI handHint;

    [SerializeField] private string uniqueId = "menu_special_button";

    void Awake()
    {
        if (targetButton == null) targetButton = GetComponent<Button>();
        if (targetButton != null) targetButton.onClick.AddListener(OnClicked);
    }

    void OnDestroy()
    {
        if (targetButton != null) targetButton.onClick.RemoveListener(OnClicked);
    }

    void Start()
    {
        StartCoroutine(TryShowWhenReady());
    }

    IEnumerator TryShowWhenReady()
    {
        while (PlayerProgress.Instance == null)
            yield return null;

        // ✅ важно: дождаться загрузки реальных данных (cloud/local)
        while (!PlayerProgress.Instance.IsLoaded)
            yield return null;

        TryShow();
    }

    void TryShow()
    {
        if (handHint == null || targetButton == null) return;
        if (string.IsNullOrEmpty(uniqueId)) return;

        // ✅ уже показывали -> никогда больше
        if (PlayerProgress.Instance.HasShownHint(uniqueId)) return;

        // ещё закрыто
        if (PlayerProgress.Instance.Level < requiredLevel) return;

        // кнопка должна быть реально доступна
        if (!targetButton.gameObject.activeInHierarchy || !targetButton.interactable) return;

        // ✅ ставим флаг СРАЗУ (в основной сейв)
        PlayerProgress.Instance.MarkHintShown(uniqueId, saveNow: true);

        handHint.ShowFor(targetButton.GetComponent<RectTransform>());
    }

    void OnClicked()
    {
        if (handHint != null) handHint.Hide();
    }
}
