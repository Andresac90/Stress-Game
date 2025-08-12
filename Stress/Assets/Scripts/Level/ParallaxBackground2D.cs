// ParallaxAnchored2D.cs
using UnityEngine;

[ExecuteAlways]
public class ParallaxAnchored2D : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Transform transform;                // Sprite/Tilemap root
        [Range(0f, 1f)] public float strength = 0.95f; // 1 = follows camera, 0 = world-fixed
        public Vector2 maxOffset = new Vector2(3f, 2f); // Clamp so it never leaves screen
        [HideInInspector] public Vector3 baseLocal;     // Initial local offset to camera
    }

    public Transform targetCamera;
    public Layer[] layers;

    Vector3 camStart;

    void OnEnable()
    {
        if (!targetCamera && Camera.main) targetCamera = Camera.main.transform;
        CacheStarts();
    }

    void OnValidate() => CacheStarts();

    void CacheStarts()
    {
        if (!targetCamera || layers == null) return;
        camStart = targetCamera.position;
        foreach (var l in layers)
        {
            if (l?.transform == null) continue;
            l.baseLocal = l.transform.position - targetCamera.position; // keep current framing
        }
    }

    void LateUpdate()
    {
        if (!targetCamera || layers == null) return;

        var camDelta = targetCamera.position - camStart; // how far the camera moved

        foreach (var l in layers)
        {
            if (l == null || !l.transform) continue;

            // Offset relative to camera. strength=1 → follow exactly; strength=0 → world fixed.
            Vector2 offset = (Vector2)camDelta * (l.strength - 1f);

            // Clamp so the layer never slides off screen.
            offset.x = Mathf.Clamp(offset.x, -l.maxOffset.x, l.maxOffset.x);
            offset.y = Mathf.Clamp(offset.y, -l.maxOffset.y, l.maxOffset.y);

            var desired = targetCamera.position + l.baseLocal + (Vector3)offset;
            l.transform.position = new Vector3(desired.x, desired.y, l.transform.position.z);
        }
    }
}
