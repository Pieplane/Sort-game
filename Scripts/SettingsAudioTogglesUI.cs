using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioTogglesUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Button sfxButton;

    [Header("Images on buttons")]
    [SerializeField] private Image musicIcon;
    [SerializeField] private Image sfxIcon;

    [Header("Sprites")]
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;

    void Awake()
    {
        if (musicButton != null) musicButton.onClick.AddListener(OnMusicClick);
        if (sfxButton != null) sfxButton.onClick.AddListener(OnSfxClick);
    }

    void OnEnable()
    {
        RefreshIcons();
    }

    private void OnMusicClick()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
            AudioManager.Instance.ToggleMusic();
        }
        
        RefreshIcons();
    }

    private void OnSfxClick()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
            AudioManager.Instance.ToggleSfx();
        }
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        var am = AudioManager.Instance;
        if (am == null) return;

        if (musicIcon != null)
            musicIcon.sprite = am.MusicEnabled ? musicOnSprite : musicOffSprite;

        if (sfxIcon != null)
            sfxIcon.sprite = am.SfxEnabled ? sfxOnSprite : sfxOffSprite;
    }
}
