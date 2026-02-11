using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    public static AudioManager Instance { get; private set; }
    //public AudioMixerGroup soundsGroup;

    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    private List<Sound> previouslyPlaying = new List<Sound>();

    // Храним активные корутины по имени звука
    private Dictionary<string, Coroutine> activeMutes = new Dictionary<string, Coroutine>();

    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;


    private Dictionary<string, float> nextAllowedTime = new Dictionary<string, float>();

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            //return;
        }
        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();

            switch (s.type)
            {
                case SoundType.Music:
                    s.source.outputAudioMixerGroup = musicGroup;
                    break;
                case SoundType.SFX:
                    s.source.outputAudioMixerGroup = sfxGroup;
                    break;
            }

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }
    IEnumerator Start()
    {
        // ждём PlayerProgress
        while (PlayerProgress.Instance == null)
            yield return null;

        // если прогресс уже загружен — применяем сразу
        ApplyFromProgress();

        // если загрузка асинхронная (WebGL) — подпишемся и применим ещё раз после SetPlayerProgress
        PlayerProgress.Instance.OnLoaded += ApplyFromProgress;
    }
    void OnDestroy()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnLoaded -= ApplyFromProgress;
    }
    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        PlayerProgress.Instance?.SetMusicOn(enabled); // ✅
        ApplyMusicState();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        PlayerProgress.Instance?.SetSfxOn(enabled);   // ✅
        ApplySfxState();
    }

    public void ToggleMusic() => SetMusicEnabled(!MusicEnabled);
    public void ToggleSfx() => SetSfxEnabled(!SfxEnabled);

    private void ApplyMusicState()
    {
        foreach (Sound s in sounds)
            if (s.type == SoundType.Music && s.source != null)
                s.source.volume = MusicEnabled ? s.volume : 0f;
    }

    private void ApplySfxState()
    {
        foreach (Sound s in sounds)
            if (s.type == SoundType.SFX && s.source != null)
                s.source.volume = SfxEnabled ? s.volume : 0f;
    }
    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) { Debug.LogWarning("Sounds: " + name + " not found!"); return; }

        if (s.type == SoundType.Music && !MusicEnabled) return;
        if (s.type == SoundType.SFX && !SfxEnabled) return;

        if (s.type == SoundType.SFX)
            s.source.PlayOneShot(s.clip, s.source.volume);
        else
            if (!s.source.isPlaying) s.source.Play();
    }
    public void StopPlay(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        // Çŕůčňŕ îň óíč÷ňîćĺííîăî îáúĺęňŕ
        if (s.source != null && s.source.gameObject != null)
        {
            s.source.Stop();
        }
    }
    public void OffMusic()
    {
        foreach (Sound s in sounds)
        {
            if (s.type == SoundType.Music && s.source != null)
            {
                s.source.volume = 0f;
            }
        }
    }
    public void OnMusic()
    {
        foreach (Sound s in sounds)
        {
            if (s.type == SoundType.Music && s.source != null)
            {
                s.source.volume = s.volume;
            }
        }
    }
    public void OffSounds()
    {
        foreach (Sound s in sounds)
        {
            if (s.type == SoundType.SFX && s.source != null)
            {
                s.source.volume = 0f;
            }
        }
    }
    public void OnSounds()
    {
        foreach (Sound s in sounds)
        {
            if (s.type == SoundType.SFX && s.source != null)
            {
                s.source.volume = s.volume;
            }
        }
    }
    public void StopAll()
    {
        foreach (Sound s in sounds)
        {
            if (!s.ignorePause && s.source != null)
            {
                s.source.Stop();
            }
        }

        // 🔸 Прерываем все активные mute-корутины
        foreach (var kvp in activeMutes)
        {
            StopCoroutine(kvp.Value);
        }
        activeMutes.Clear();
    }
    public void StopAllExceptBub()
    {
        previouslyPlaying.Clear();

        foreach (Sound s in sounds)
        {
            if (s.source != null && s.source.isPlaying && s.name != "Bub")
            {
                previouslyPlaying.Add(s);
                s.source.Stop();
            }
        }
    }
    public bool IsPlaying(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sounds: " + name + " not found!");
            return false;
        }

        return s.source.isPlaying;
    }
    public void ResumePrevious()
    {
        foreach (Sound s in previouslyPlaying)
        {
            if (s.source != null)
                s.source.Play();
        }

        previouslyPlaying.Clear();
    }
    public void PlayClipInSound(string soundName, AudioClip clip, float volume = 1f, Vector3? position = null)
    {
        Sound s = Array.Find(sounds, sound => sound.name == soundName);
        if (s == null || clip == null)
        {
            Debug.LogWarning("Sound " + soundName + " not found or clip is null.");
            return;
        }

        s.source.clip = clip;
        s.source.volume = volume;

        if (position.HasValue)
        {
            // 3D-sound
            s.source.transform.position = position.Value;
            s.source.spatialBlend = 1f;
        }
        else
        {
            // 2D-sound
            s.source.spatialBlend = 0f;
        }

        s.source.Play();
    }
    // 🔇 Временный мут звука
    public void MuteSoundTemporarily(string name, float duration, float fadeTime = 0.3f)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null || s.source == null) return;

        // 🚫 Если пользователь сам отключил музыку — не трогаем
        //if (s.type == SoundType.Music && !Progress.Instance.PlayerInfo.IsMusicOn)
        //    return;

        // Если уже есть активная корутина для этого звука — останавливаем её
        if (activeMutes.TryGetValue(name, out Coroutine existing))
        {
            StopCoroutine(existing);
            activeMutes.Remove(name);
        }

        Coroutine newRoutine = StartCoroutine(MuteSoundCoroutine(name, duration, fadeTime));
        activeMutes[name] = newRoutine;
    }

    private IEnumerator MuteSoundCoroutine(string name, float duration, float fadeTime)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null || s.source == null)
        {
            Debug.LogWarning($"[AudioManager] Sound '{name}' not found or has no AudioSource!");
            yield break;
        }

        float originalVolume = s.source.volume;

        // Плавное затухание
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            s.source.volume = Mathf.Lerp(originalVolume, 0f, t / fadeTime);
            yield return null;
        }
        s.source.volume = 0f;

        // Ждём указанное время
        yield return new WaitForSeconds(duration);

        // Плавное возвращение
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            s.source.volume = Mathf.Lerp(0f, originalVolume, t / fadeTime);
            yield return null;
        }
        s.source.volume = originalVolume;

        // Убираем запись из активных мутов
        activeMutes.Remove(name);
    }
    public void UnmuteSound(string name, float fadeTime = 0.3f)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null || s.source == null)
            return;

        // 🚫 Если музыка выключена пользователем — не включаем обратно
        //if (s.type == SoundType.Music && !Progress.Instance.PlayerInfo.IsMusicOn)
        //    return;

        // Если есть активная корутина — останавливаем её
        if (activeMutes.TryGetValue(name, out Coroutine existing))
        {
            StopCoroutine(existing);
            activeMutes.Remove(name);
        }

        StartCoroutine(UnmuteSoundCoroutine(name, fadeTime));
    }

    private IEnumerator UnmuteSoundCoroutine(string name, float fadeTime)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null || s.source == null)
        {
            Debug.LogWarning($"[AudioManager] Sound '{name}' not found or has no AudioSource!");
            yield break;
        }

        float targetVolume = s.volume;
        float currentVolume = s.source.volume;

        if (fadeTime <= 0f)
        {
            s.source.volume = targetVolume;
            yield break;
        }

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            s.source.volume = Mathf.Lerp(currentVolume, targetVolume, t / fadeTime);
            yield return null;
        }
        s.source.volume = targetVolume;
    }
    public void PlayMusic(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) { Debug.LogWarning("Music: " + name + " not found!"); return; }
        if (s.type != SoundType.Music) { Debug.LogWarning($"{name} is not Music"); return; }
        if (!MusicEnabled) return;

        // стопаем всю музыку кроме этой
        foreach (var m in sounds)
            if (m.type == SoundType.Music && m.source != null && m.name != name)
                m.source.Stop();

        if (!s.source.isPlaying)
            s.source.Play();
    }

    public void StopMusic(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null || s.source == null) return;
        if (s.type != SoundType.Music) return;
        s.source.Stop();
    }
    public void PlayCooldown(string name, float cooldown)
    {
        float now = Time.unscaledTime;

        // узнаем, когда этот звук можно играть снова
        nextAllowedTime.TryGetValue(name, out float nextTime);

        // если ещё рано — выходим
        if (now < nextTime)
            return;

        // разрешаем следующий запуск через cooldown секунд
        nextAllowedTime[name] = now + cooldown;

        // проигрываем как обычно
        Play(name);
    }
    void ApplyFromProgress()
    {
        if (PlayerProgress.Instance == null) return;

        MusicEnabled = PlayerProgress.Instance.MusicOn;
        SfxEnabled = PlayerProgress.Instance.SfxOn;

        ApplyMusicState();
        ApplySfxState();
    }
}
