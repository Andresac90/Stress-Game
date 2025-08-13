using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Range(0f, 1f)] public float masterVolume = 1f;

    private AudioSource _a, _b;
    private AudioSource _active;
    private Coroutine _xfade;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _a, _b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D
            s.volume = 0f;
        }
        _active = _a;
    }

    public void Play(AudioClip clip, float fadeSeconds = 1f, float targetVolume = 1f)
    {
        if (!clip) return;

        var next = (_active == _a) ? _b : _a;
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        if (_xfade != null) StopCoroutine(_xfade);
        _xfade = StartCoroutine(Crossfade(next, fadeSeconds, Mathf.Clamp01(targetVolume)));
    }

    public void Stop(float fadeSeconds = 0.5f)
    {
        if (_xfade != null) StopCoroutine(_xfade);
        _xfade = StartCoroutine(FadeOutAll(fadeSeconds));
    }

    private IEnumerator Crossfade(AudioSource next, float seconds, float targetVol)
    {
        var prev = _active;
        _active = next;

        float t = 0f;
        float startPrev = prev.volume;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; 
            float k = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
            prev.volume = masterVolume * Mathf.Lerp(startPrev, 0f, k);
            next.volume = masterVolume * Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }
        prev.Stop();
        prev.volume = 0f;
        next.volume = masterVolume * targetVol;
        _xfade = null;
    }

    private IEnumerator FadeOutAll(float seconds)
    {
        var s1 = _a; var s2 = _b;
        float t = 0f;
        float v1 = s1.volume, v2 = s2.volume;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
            s1.volume = masterVolume * Mathf.Lerp(v1, 0f, k);
            s2.volume = masterVolume * Mathf.Lerp(v2, 0f, k);
            yield return null;
        }
        s1.Stop(); s2.Stop();
        s1.volume = s2.volume = 0f;
        _xfade = null;
    }
}
