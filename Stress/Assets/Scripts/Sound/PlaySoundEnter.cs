// PlayFootstep.cs
using UnityEngine;

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

    public void PlaySoundWithVolume(float v)
    {
        SoundManager.PlaySound(sound, Mathf.Clamp01(v), overlap);
    }
}
