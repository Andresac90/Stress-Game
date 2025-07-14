// HookPool.cs
using UnityEngine;
using System.Collections.Generic;

public class HookPool : MonoBehaviour
{
    public static HookPool Instance { get; private set; }

    [Tooltip("Drag your GrapplingHook2D prefab here")]
    public GrapplingHook2D hookPrefab;

    [Tooltip("Initial number of hooks to pre-spawn")]
    public int poolSize = 5;

    private Queue<GrapplingHook2D> pool = new Queue<GrapplingHook2D>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-spawn
        for (int i = 0; i < poolSize; i++)
        {
            var h = Instantiate(hookPrefab);
            h.gameObject.SetActive(false);
            pool.Enqueue(h);
        }
    }

    public GrapplingHook2D GetHook()
    {
        if (pool.Count > 0)
        {
            var h = pool.Dequeue();
            h.gameObject.SetActive(true);
            return h;
        }
        // pool exhausted → expand
        return Instantiate(hookPrefab);
    }

    public void ReturnHook(GrapplingHook2D hook)
    {
        hook.gameObject.SetActive(false);
        pool.Enqueue(hook);
    }
}
