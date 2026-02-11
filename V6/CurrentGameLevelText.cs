using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentGameLevelText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private string format = "{0} {1}";

    void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        StartCoroutine(Bind());
    }

    void OnDisable()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnGameLevelChanged -= OnChanged;
    }

    void OnChanged(int gameLevel)
    {
        if (text == null) return;

        string lang = (Language.Instance != null && !string.IsNullOrEmpty(Language.Instance.CurrentLanguage))
            ? Language.Instance.CurrentLanguage.ToLowerInvariant()
            : "en";

        string levelWord = GetLevelWord(lang);

        // ✅ ВАЖНО: форматируем format
        text.text = string.Format(format, levelWord, gameLevel);
    }
    string GetLevelWord(string lang)
    {
        if (lang.Contains("ru")) return "Уровень";
        if (lang.Contains("en")) return "Level";
        if (lang.Contains("fr")) return "Niveau";
        if (lang.Contains("de")) return "Level";

        // дефолт — русский
        return "Level";
    }
    IEnumerator Bind()
    {
        while (PlayerProgress.Instance == null)
            yield return null;

        PlayerProgress.Instance.OnGameLevelChanged += OnChanged;
        OnChanged(PlayerProgress.Instance.GameLevel);
    }
}
