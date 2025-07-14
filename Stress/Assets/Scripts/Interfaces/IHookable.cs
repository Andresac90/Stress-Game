// IHookable.cs
public interface IHookable
{
    /// <summary>
    /// Called by the hook when it latches onto this object.
    /// </summary>
    /// <param name="hookPoint">World-space point where the hook hit.</param>
    void OnHooked(UnityEngine.Vector2 hookPoint);
}
