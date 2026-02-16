using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    Music,
    SFX,
}
[System.Serializable]
public class Sound
{
    public string name;

    public SoundType type;

    public AudioClip clip;

    public bool ignorePause;

    [Range(0f, 1f)]
    public float volume;
    [Range(.1f, 3f)]
    public float pitch;

    public bool loop;

    [HideInInspector]
    public AudioSource source;
}
