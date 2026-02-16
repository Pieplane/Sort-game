using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    [Tooltip("Имена Sound в AudioManager с type=Music")]
    [SerializeField]
    private List<string> musicTracks = new List<string>
    {
        "Music1", "Music2", "Music3"
    };

    [SerializeField] private int changeEveryLevels = 4;
    [SerializeField] private bool playOnStart = true;

    private string current;

    void Start()
    {
        if (playOnStart)
            ApplyForLevel(GameFlow.GameLevel);
    }

    public void ApplyForLevel(int gameLevel)
    {
        if (AudioManager.Instance == null) return;
        if (musicTracks == null || musicTracks.Count == 0) return;

        int levelIndex = Mathf.Max(1, gameLevel) - 1;
        int block = levelIndex / Mathf.Max(1, changeEveryLevels);
        int trackIndex = block % musicTracks.Count;

        string next = musicTracks[trackIndex];
        if (next == current) return;

        // переключаем
        current = next;
        AudioManager.Instance.PlayMusic(current);
    }
}
