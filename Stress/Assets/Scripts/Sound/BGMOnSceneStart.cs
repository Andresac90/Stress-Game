using UnityEngine;

public class BGMOnSceneStart : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private float fadeInSeconds = 1f;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;

    void Start()
    {
        if (!MusicManager.Instance)
        {
            var mm = new GameObject("MusicManager").AddComponent<MusicManager>();
        }
        MusicManager.Instance.Play(music, fadeInSeconds, volume);
    }
}
