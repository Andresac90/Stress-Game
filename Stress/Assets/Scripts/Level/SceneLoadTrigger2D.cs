// SceneLoadTrigger2D.cs 
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneLoadTrigger2D : MonoBehaviour
{
    [Tooltip("Scene name to load when the player enters this area (must be in Build Settings).")]
    public string sceneToLoad;

    [Tooltip("Optional: player's tag to verify. Leave empty to skip.")]
    public string requiredTag = "Player";

    [Tooltip("Optional: point to test (e.g., player's GroundCheck). If empty, uses the player's transform position.")]
    public Transform pointOverride;

    private Collider2D _area;
    private Transform _player;
    private bool _fired;

    void Awake()
    {
        _area = GetComponent<Collider2D>();
        if (!_area) enabled = false;
    }

    void Start()
    {
        if (!_player)
        {
            var pc = FindObjectOfType<PlayerController2D>();
            if (pc) _player = pc.transform;
        }
    }

    void Update()
    {
        if (_fired || !_area) return;

        // Find player if not already cached
        if (!_player)
        {
            var pc = FindObjectOfType<PlayerController2D>();
            if (!pc) return;
            _player = pc.transform;
        }

        if (!string.IsNullOrEmpty(requiredTag) && !_player.CompareTag(requiredTag))
            return;

        // World-space point to test against the collider volume
        Vector2 probe = pointOverride ? (Vector2)pointOverride.position : (Vector2)_player.position;

        // OverlapPoint works on triggers and non-triggers; no RB needed on the player
        if (_area.OverlapPoint(probe))
        {
            _fired = true;
            if (!string.IsNullOrEmpty(sceneToLoad))
                SceneManager.LoadScene(sceneToLoad);
            else
                Debug.LogWarning("[SceneLoadTrigger2D] Scene name not set.");
        }
    }

#if UNITY_EDITOR
    // Visualize the trigger volume
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (!col) return;
        var b = col.bounds;
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
