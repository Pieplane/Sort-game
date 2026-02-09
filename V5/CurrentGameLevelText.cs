using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentGameLevelText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private string format = "Уровень {0}";

    void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        GameFlow.OnGameLevelChanged += OnChanged;
        OnChanged(GameFlow.GameLevel); // сразу нарисовать текущее
    }

    void OnDisable()
    {
        GameFlow.OnGameLevelChanged -= OnChanged;
    }

    void OnChanged(int gameLevel)
    {
        if (text == null) return;
        text.text = string.Format(format, gameLevel);
    }
}
