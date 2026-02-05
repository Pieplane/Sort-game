using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private PopupAnimator animator;

    [Header("Buttons")]
    [SerializeField] private Button openButton;  // кнопка в HUD (шестерёнка)
    [SerializeField] private Button closeButton; // X на панели
    //[SerializeField] private Button overlayButton; // если хочешь закрывать по клику по фону (опционально)

    void Awake()
    {

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // опционально: клик по затемнению закрывает
        //if (overlayButton != null) overlayButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        animator?.Show();
        //Time.timeScale = 0f; // если хочешь паузу в настройках
    }

    public void Close()
    {
        //Time.timeScale = 1f;
        animator?.Hide();
    }
}
