// SoundManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny global SFX helper. Maps <see cref="SoundType"/> enum to clips and
/// plays them with optional overlap policies (Poly, Cut, SkipIfPlaying).
/// Keep clip order in the Inspector matched to the SoundType enum order.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    /// <summary>
    /// How to handle multiple plays of the same sound.
    /// Poly: allow overlap (PlayOneShot).
    /// Cut:  stop any existing voice for that sound and play the new one.
    /// SkipIfPlaying: do nothing if that sound is already playing.
    /// </summary>
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
    private AudioSource oneShotSource; // used for Poly (PlayOneShot)

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

    /// <summary>
    /// Backward-compatible API. Uses Poly (overlap allowed).
    /// </summary>
    public static void PlaySound(SoundType sound, float volume = 1f)
        => PlaySound(sound, volume, SoundOverlap.Poly);

    /// <summary>
    /// Plays a sound using the provided overlap policy.
    /// </summary>
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

    /// <summary>Stops a specific sound if it has a dedicated voice.</summary>
    public static void StopSound(SoundType sound)
    {
        if (instance == null) return;
        if (instance.voices.TryGetValue(sound, out var v) && v) v.Stop();
    }

    /// <summary>Stops all dedicated voices (Cut/SkipIfPlaying sounds).</summary>
    public static void StopAll()
    {
        if (instance == null) return;
        foreach (var kvp in instance.voices)
            if (kvp.Value) kvp.Value.Stop();
    }

    /// <summary>Set master SFX volume (0..1).</summary>
    public static void SetMasterVolume(float v)
    {
        if (instance == null) return;
        instance.masterVolume = Mathf.Clamp01(v);
    }

    /// <summary>Enable/disable 2D forcing for all sources (applies to future voices).</summary>
    public static void SetForce2D(bool enabled)
    {
        if (instance == null) return;
        instance.force2D = enabled;
        if (instance.oneShotSource) instance.oneShotSource.spatialBlend = enabled ? 0f : instance.oneShotSource.spatialBlend;
        foreach (var v in instance.voices.Values)
            if (v) v.spatialBlend = enabled ? 0f : v.spatialBlend;
    }

    // --- Internals ---

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
