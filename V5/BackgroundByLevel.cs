using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundByLevel : MonoBehaviour
{
    [Header("Sprites by level range")]
    [SerializeField] private Sprite sprite_1_8;
    [SerializeField] private Sprite sprite_9_15;
    [SerializeField] private Sprite sprite_16_plus;

    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Debug")]
    [SerializeField] private bool log;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        GameFlow.OnGameLevelChanged += HandleLevelChanged;
        Refresh(GameFlow.GameLevel); // обновить сразу при входе в сцену/активации
    }

    void OnDisable()
    {
        GameFlow.OnGameLevelChanged -= HandleLevelChanged;
    }

    void HandleLevelChanged(int newLevel)
    {
        Refresh(newLevel);
    }

    void Refresh(int level)
    {
        level = Mathf.Max(1, level);

        Sprite chosen =
            (level <= 8) ? sprite_1_8 :
            (level <= 15) ? sprite_9_15 :
            sprite_16_plus;

        if (targetImage != null && targetImage.sprite != chosen)
            targetImage.sprite = chosen;

        //if (log)
        //    Debug.Log($"[BackgroundByGameLevel] level={level} -> {(chosen ? chosen.name : "NULL")}", this);
    }
}
