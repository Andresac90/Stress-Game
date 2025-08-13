// LevelIntroCinematic_BrainOverride.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; 

//Runs a start-of-level flythrough using the Main Camera transform
//Temporarily disables CinemachineBrain so we can drive the camera directly,
//then re-enables it and returns control to the player's Cinemachine camera.
[DisallowMultipleComponent]
public class LevelIntroCinematic : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Player to lock input during the cinematic. If empty, auto-finds PlayerController2D.")]
    public PlayerController2D player;

    [Header("Path (world-space)")]
    [Tooltip("Ordered waypoints the camera will visit.")]
    public Transform[] waypoints;

    [Header("Timing")]
    [Tooltip("Seconds per segment between waypoints.")]
    public float secondsPerSegment = 2f;
    [Tooltip("Pause at the final waypoint.")]
    public float holdAtEnd = 0.75f;
    [Tooltip("Seconds to return back to the starting camera position.")]
    public float returnDuration = 1.25f;
    [Tooltip("Motion easing per segment.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Behavior")]
    public bool playOnStart = true;
    public bool allowSkip = true;

    // internals
    CinemachineBrain _brain;
    Transform _cam;
    Vector3 _startPos;
    float _camZ = -10f;
    bool _running;

    void Awake()
    {
        if (!player) player = FindObjectOfType<PlayerController2D>();

        if (Camera.main)
        {
            _cam = Camera.main.transform;
            _brain = _cam.GetComponent<CinemachineBrain>();
            _camZ = _cam.position.z;
        }
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (_running || _cam == null || waypoints == null || waypoints.Length < 2)
        {
            if (_cam == null) Debug.LogWarning("No Main Camera found.");
            if (waypoints == null || waypoints.Length < 2) Debug.LogWarning("Need at least 2 waypoints.");
            return;
        }

        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        _running = true;

        // Lock player input
        if (player)
        {
            player.moveAction.action.Disable();
            player.jumpAction.action.Disable();
            player.fireAction.action.Disable();
            player.pointerAction.action.Disable();
            player.velocity = Vector2.zero;
        }

        // Take control of the camera
        _startPos = _cam.position;
        if (_brain) _brain.enabled = false;

        // Snap to first waypoint
        Vector3 first = waypoints[0].position; first.z = _camZ;
        _cam.position = first;

        // Traverse each segment
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 a = waypoints[i].position; a.z = _camZ;
            Vector3 b = waypoints[i + 1].position; b.z = _camZ;

            float t = 0f;
            while (t < 1f)
            {
                if (allowSkip && AnySkipPressed()) { yield return ReturnQuick(); goto DONE; }
                t += Time.deltaTime / Mathf.Max(0.0001f, secondsPerSegment);
                float k = ease.Evaluate(Mathf.Clamp01(t));
                _cam.position = Vector3.Lerp(a, b, k);
                yield return null;
            }
        }

        // Hold at the end
        float hold = 0f;
        while (hold < holdAtEnd)
        {
            if (allowSkip && AnySkipPressed()) { yield return ReturnQuick(); goto DONE; }
            hold += Time.deltaTime;
            yield return null;
        }

        // Return to start
        {
            Vector3 end = _cam.position;
            float t = 0f;
            while (t < 1f)
            {
                if (allowSkip && AnySkipPressed()) break;
                t += Time.deltaTime / Mathf.Max(0.0001f, returnDuration);
                float k = ease.Evaluate(Mathf.Clamp01(t));
                _cam.position = Vector3.Lerp(end, _startPos, k);
                yield return null;
            }
        }

    DONE:
        // Give control back to Cinemachine + player input
        if (_brain) _brain.enabled = true;

        if (player)
        {
            player.moveAction.action.Enable();
            player.jumpAction.action.Enable();
            player.fireAction.action.Enable();
            player.pointerAction.action.Enable();
        }

        _running = false;
    }

    IEnumerator ReturnQuick()
    {
        Vector3 end = _cam.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            _cam.position = Vector3.Lerp(end, _startPos, k);
            yield return null;
        }
    }

    static bool AnySkipPressed()
    {
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame)
            || (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame || ms.middleButton.wasPressedThisFrame))
            || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame));
    }
}
