using System.Numerics;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Input;

public enum Scancode
{

}

public enum Keycode
{

}

public enum KeyModifier
{

}

public enum MouseButton
{

}

/// <summary>
/// Represents a recording of a Key Event
/// </summary>
/// <param name="Scancode">The Scancode of the key</param>
/// <param name="Key">The actual value of the key</param>
/// <param name="Modifiers">The modifiers that were active at the time of this event</param>
/// <param name="IsPressed">Whether the key was pressed or not</param>
/// <param name="Repeat">Whether the key was a repeat press or not</param>
/// <param name="Unicode">The unicode value of the key</param>
public readonly record struct KeyEventRecord(Scancode Scancode, Keycode Key, KeyModifier Modifiers, bool IsPressed, bool Repeat, uint Unicode);

/// <summary>
/// Represents a recording of a Mouse Event
/// </summary>
/// <param name="Delta">The amount of travel the Mouse had during this event</param>
/// <param name="NewPosition">The new position of the mouse</param>
/// <param name="Pressed">The buttons that were pressed at the time of this event</param>
public readonly record struct MouseEventRecord(Vector2 Delta, Vector2 NewPosition, MouseButton Pressed);

/// <summary>
/// Represents a snapshot of given input at a specific frame
/// </summary>
/// <remarks>
/// Input events get recorded at all times, and are reset at the beginning of a new frame. Disposing this object returns it to the Snapshot pool, not doing so may result in extra allocations
/// </remarks>
public sealed class InputSnapshot : IDisposable
{
    internal readonly GraphicsManager Manager;
    internal InputSnapshot(GraphicsManager manager)
    {
        Manager = manager;
    }

    /// <summary>
    /// The amount of time that SDL had been active at the time of this <see cref="InputSnapshot"/>'s last update
    /// </summary>
    public TimeSpan LastUpdated { get; internal set; }

    /// <summary>
    /// The key events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<KeyEventRecord> KeyEvents => kEvs;
    internal List<KeyEventRecord> kEvs = new();

    /// <summary>
    /// The characters that were pressed at the time of this snapshot
    /// </summary>
    public IReadOnlyList<uint> KeyCharPresses => kcEvs;
    internal List<uint> kcEvs = new();

    /// <summary>
    /// The characters that were pressed at the time of this snapshot
    /// </summary>
    public IReadOnlyList<MouseEventRecord> MouseEvents => mEvs;
    internal List<MouseEventRecord> mEvs = new();

    /// <summary>
    /// The mouse position at the time of this snapshot
    /// </summary>
    public Vector2 MousePosition { get; internal set; }

    /// <summary>
    /// The mouse wheel's vertical delta at the time of this snapshot
    /// </summary>
    public float WheelVerticalDelta { get; internal set; }

    /// <summary>
    /// The mouse wheel's horizontal delta at the time of this snapshot
    /// </summary>
    public float WheelHorizontalDelta { get; internal set; }

    /// <summary>
    /// Checks if <paramref name="button"/> was pressed at the time of this snapshot
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
    public bool IsMouseDown(MouseButton button) => butt.HasFlag(button);
    internal MouseButton butt;

    /// <summary>
    /// Returns this <see cref="InputSnapshot"/> to the Snapshot pool so that it may be reused without a new allocation
    /// </summary>
    /// <remarks>
    /// Synonymous with this object's <see cref="IDisposable.Dispose"/>
    /// </remarks>
    public void ReturnToPool()
        => Manager.ReturnSnapshot(this);

    internal void FetchLastMomentData()
    {
        var ms = Mouse.MouseState;
        MousePosition = new(ms.Location.X, ms.Location.Y);
    }

    internal void Clear()
    {
        WheelHorizontalDelta = 0;
        WheelVerticalDelta = 0;
        MousePosition = default;
        kEvs.Clear();
        kcEvs.Clear();
        mEvs.Clear();
    }

    internal void CopyTo(InputSnapshot other)
    {
        other.butt = butt;
        other.WheelHorizontalDelta = WheelHorizontalDelta;
        other.WheelVerticalDelta = WheelVerticalDelta;
        other.MousePosition = MousePosition;

        other.kEvs.Clear();
        other.kEvs.EnsureCapacity(kEvs.Capacity);
        for (int i = 0; i < kEvs.Count; i++)
            other.kEvs.Add(kEvs[i]);

        other.kcEvs.Clear();
        other.kcEvs.EnsureCapacity(kcEvs.Capacity);
        for (int i = 0; i < kcEvs.Count; i++)
            other.kcEvs.Add(kcEvs[i]);

        other.mEvs.Clear();
        other.mEvs.EnsureCapacity(mEvs.Capacity);
        for (int i = 0; i < mEvs.Count; i++)
            other.mEvs.Add(mEvs[i]);
    }

    void IDisposable.Dispose() => ReturnToPool();
}
