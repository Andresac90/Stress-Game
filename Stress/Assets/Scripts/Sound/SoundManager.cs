using System;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    SWORD,
    GRAPPLING_HOOK,
    LAND,
    JUMP,
    FOOTSTEP,
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance = null;
    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume); 
    }
}