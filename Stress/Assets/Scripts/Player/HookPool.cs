// HookPool.cs
using UnityEngine;
using System.Collections.Generic;

// Lightweight pool for GrapplingHook2D instances
[DisallowMultipleComponent]
public class HookPool : MonoBehaviour
{
    //Global access to the pool instance
    public static HookPool Instance { get; private set; }

    [Tooltip("Prefab to instantiate for pooled hooks.")]
    public GrapplingHook2D hookPrefab;

    [Tooltip("Initial number of hooks to pre-spawn.")]
    public int poolSize = 5;

    private readonly Queue<GrapplingHook2D> pool = new Queue<GrapplingHook2D>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-spawn
        for (int i = 0; i < Mathf.Max(0, poolSize); i++)
        {
            var h = Instantiate(hookPrefab);
            h.gameObject.SetActive(false);
            pool.Enqueue(h);
        }
    }

    //Gets a hook from the pool (or instantiates if empty)
    public GrapplingHook2D GetHook()
    {
        if (pool.Count > 0)
        {
            var h = pool.Dequeue();
            h.gameObject.SetActive(true);
            return h;
        }
        return Instantiate(hookPrefab);
    }

    //Returns a hook to the pool and disables it
    public void ReturnHook(GrapplingHook2D hook)
    {
        if (!hook) return;
        hook.gameObject.SetActive(false);
        pool.Enqueue(hook);
    }
}
