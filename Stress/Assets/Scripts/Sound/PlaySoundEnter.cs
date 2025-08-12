// PlayFootstep.cs
using UnityEngine;

/// <summary>
/// Animation Event helper to trigger footstep SFX without stacking.
/// Place this on the same GameObject that receives the animation events.
/// </summary>
public class PlayFootstep : MonoBehaviour
{
    [Header("Footstep Settings")]
    [SerializeField] private SoundType sound = SoundType.FOOTSTEP;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [SerializeField] private SoundManager.SoundOverlap overlap = SoundManager.SoundOverlap.SkipIfPlaying;


    public void PlaySound()
    {
        SoundManager.PlaySound(sound, volume, overlap);
    }

    /// <summary>
    /// Optional Animation Event that passes a float volume (0..1).
    /// In your clip's event, set Function = PlaySoundWithVolume and provide a float.
    /// </summary>
    public void PlaySoundWithVolume(float v)
    {
        SoundManager.PlaySound(sound, Mathf.Clamp01(v), overlap);
    }
}
