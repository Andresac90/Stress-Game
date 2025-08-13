// SoundManager.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public enum SoundOverlap { Poly, Cut, SkipIfPlaying }

    [Header("Configuration")]
    [Tooltip("Assign clips in the same order as the SoundType enum.")]
    [SerializeField] private AudioClip[] soundList;

    [Range(0f, 1f)]
    [Tooltip("Global multiplier applied to all SFX.")]
    [SerializeField] private float masterVolume = 1f;

    [Tooltip("Force SFX to be 2D (no spatialization). Good for UI/precise timing.")]
    [SerializeField] private bool force2D = true;

    private static SoundManager instance;
    private AudioSource oneShotSource;

    // Dedicated voices for Cut / SkipIfPlaying so we can control a sound per type
    private readonly Dictionary<SoundType, AudioSource> voices =
        new Dictionary<SoundType, AudioSource>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        oneShotSource = GetComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        if (force2D) oneShotSource.spatialBlend = 0f; // 2D for consistent timing
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
        => PlaySound(sound, volume, SoundOverlap.Poly);

    public static void PlaySound(SoundType sound, float volume, SoundOverlap overlap)
    {
        if (instance == null || instance.soundList == null) return;

        int index = (int)sound;
        if ((uint)index >= instance.soundList.Length) return;

        var clip = instance.soundList[index];
        if (!clip) return;

        float vol = Mathf.Clamp01(instance.masterVolume * volume);

        switch (overlap)
        {
            case SoundOverlap.Cut:
                {
                    var v = instance.EnsureVoice(sound);
                    v.Stop();
                    v.clip = clip;
                    v.volume = vol;
                    v.Play();
                    break;
                }
            case SoundOverlap.SkipIfPlaying:
                {
                    var v = instance.EnsureVoice(sound);
                    if (v.isPlaying) return;
                    v.clip = clip;
                    v.volume = vol;
                    v.Play();
                    break;
                }
            case SoundOverlap.Poly:
            default:
                {
                    instance.oneShotSource.PlayOneShot(clip, vol);
                    break;
                }
        }
    }

    public static void StopSound(SoundType sound)
    {
        if (instance == null) return;
        if (instance.voices.TryGetValue(sound, out var v) && v) v.Stop();
    }

    public static void StopAll()
    {
        if (instance == null) return;
        foreach (var kvp in instance.voices)
            if (kvp.Value) kvp.Value.Stop();
    }

    public static void SetMasterVolume(float v)
    {
        if (instance == null) return;
        instance.masterVolume = Mathf.Clamp01(v);
    }

    public static void SetForce2D(bool enabled)
    {
        if (instance == null) return;
        instance.force2D = enabled;
        if (instance.oneShotSource) instance.oneShotSource.spatialBlend = enabled ? 0f : instance.oneShotSource.spatialBlend;
        foreach (var v in instance.voices.Values)
            if (v) v.spatialBlend = enabled ? 0f : v.spatialBlend;
    }


    private AudioSource EnsureVoice(SoundType type)
    {
        if (voices.TryGetValue(type, out var src) && src) return src;

        var go = new GameObject($"SFX_{type}");
        go.transform.SetParent(transform, false);

        var voice = go.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        if (force2D) voice.spatialBlend = 0f;

        voices[type] = voice;
        return voice;
    }
}
