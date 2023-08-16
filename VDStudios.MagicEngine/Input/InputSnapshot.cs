using System.Numerics;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Input;

/// <summary>
/// Represents a recording of a Key Event
/// </summary>
/// <param name="Scancode">The Scancode of the key</param>
/// <param name="Key">The actual value of the key</param>
/// <param name="Modifiers">The modifiers that were active at the time of this event</param>
/// <param name="IsPressed">Whether the key was pressed or not</param>
/// <param name="Repeat">Whether the key was a repeat press or not</param>
/// <param name="Unicode">The unicode value of the key</param>
/// <param name="KeyboardId">The keyboard's ID if applicable</param>
public readonly record struct KeyEventRecord(uint KeyboardId, Scancode Scancode, Keycode Key, KeyModifier Modifiers, bool IsPressed, bool Repeat, uint Unicode);

/// <summary>
/// Represents a recording of a Mouse Event
/// </summary>
/// <param name="Delta">The amount of travel the Mouse had during this event</param>
/// <param name="NewPosition">The new position of the mouse</param>
/// <param name="Pressed">The buttons that were pressed at the time of this event</param>
/// <param name="MouseId">The mouse's Id if applicable</param>
public readonly record struct MouseEventRecord(uint MouseId, Vector2 Delta, Vector2 NewPosition, MouseButton Pressed);

/// <summary>
/// Represents a recording of a Mouse Wheel Event
/// </summary>
/// <param name="MouseId">The mouse's Id if applicable</param>
/// <param name="WheelDelta">The amount of travel the Mouse's wheel had in each axis during this event.</param>
public readonly record struct MouseWheelEventRecord(uint MouseId, Vector2 WheelDelta);

/// <summary>
/// Represents a snapshot of given input at a specific frame
/// </summary>
/// <remarks>
/// Input events get recorded at all times, and are reset at the beginning of a new frame. Disposing this object returns it to the Snapshot pool, not doing so may result in extra allocations
/// </remarks>
public abstract class InputSnapshot : IDisposable
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
    /// The mouse events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<MouseEventRecord> MouseEvents => mEvs;
    internal List<MouseEventRecord> mEvs = new();

    /// <summary>
    /// The mouse wheel events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<MouseWheelEventRecord> MouseWheelEvents => mwEvs;
    internal List<MouseWheelEventRecord> mwEvs = new();

    /// <summary>
    /// The mouse position at the time of this snapshot
    /// </summary>
    public Vector2 MousePosition { get; internal set; }

    /// <summary>
    /// The mouse wheel's delta at the time of this snapshot
    /// </summary>
    public Vector2 WheelDelta { get; internal set; }

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

    /// <summary>
    /// Fetches data that can only be obtained at the last possible moment, like the Mouse's current position
    /// </summary>
    protected internal virtual void FetchLastMomentData()
    {
        MousePosition = FetchMousePosition();
    }

    /// <summary>
    /// Fetches the mouse's current position
    /// </summary>
    /// <remarks>
    /// The mouse's position is assumed to be in window-space, and not necessarily screen-space
    /// </remarks>
    /// <returns>A <see cref="Vector2"/> representing the mouse's current position in the screen</returns>
    protected abstract Vector2 FetchMousePosition();

    /// <summary>
    /// Reports a mouse wheel event
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="delta">A <see cref="Vector2"/> representing how much did the mouse wheel move in each axis</param>
    protected internal virtual void ReportMouseWheelMoved(uint mouseId, Vector2 delta)
    {
        WheelDelta += delta;
        mwEvs.Add(new MouseWheelEventRecord(mouseId, delta));
    }

    /// <summary>
    /// Reports a mouse event
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="delta">A <see cref="Vector2"/> representing how much did the mouse move in each axis</param>
    /// <param name="newPosition">The mouse's current position at the time of this event</param>
    /// <param name="pressed">The buttons that were pressed at the time of this event</param>
    protected internal virtual void ReportMouseMoved(uint mouseId, Vector2 delta, Vector2 newPosition, MouseButton pressed)
    {
        mEvs.Add(new(mouseId, delta, newPosition, pressed));
    }

    /// <summary>
    /// Reports a mouse event
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="clicks">The amount of clicks, if any, that were done in this event</param>
    /// <param name="state">The mouse's current state</param>
    protected internal virtual void ReportMouseButtonReleased(uint mouseId, int clicks, MouseButton state)
    {
        butt &= ~state;
        mEvs.Add(new(mouseId, Vector2.Zero, FetchMousePosition(), state));
    }

    /// <summary>
    /// Reports a mouse event
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="clicks">The amount of clicks, if any, that were done in this event</param>
    /// <param name="state">The mouse's current state</param>
    protected internal virtual void ReportMouseButtonPressed(uint mouseId, int clicks, MouseButton state)
    {
        butt |= state;
        mEvs.Add(new(mouseId, Vector2.Zero, FetchMousePosition(), state));
    }

    /// <summary>
    /// Reports a keyboard event
    /// </summary>
    /// <param name="keyboardId">The id of the keyboard, if applicable</param>
    /// <param name="scancode">The scancode of the key released</param>
    /// <param name="key">The keycode of the key released</param>
    /// <param name="modifiers">Any modifiers that may have been released at the time this key was released</param>
    /// <param name="isPressed">Whether the key was released at the time of this event. Usually <see langword="false"/></param>
    /// <param name="repeat">Whether this keypress is a repeat, i.e. it's being held down</param>
    /// <param name="unicode">The unicode value for key that was released</param>
    protected internal virtual void ReportKeyReleased(uint keyboardId, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        kEvs.Add(new(keyboardId, scancode, key, modifiers, false, repeat, unicode));
    }

    /// <summary>
    /// Reports a keyboard event
    /// </summary>
    /// <param name="keyboardId">The id of the keyboard, if applicable</param>
    /// <param name="scancode">The scancode of the key pressed</param>
    /// <param name="key">The keycode of the key pressed</param>
    /// <param name="modifiers">Any modifiers that may have been pressed at the time this key was pressed</param>
    /// <param name="isPressed">Whether the key was pressed at the time of this event. Usually <see langword="true"/></param>
    /// <param name="repeat">Whether this keypress is a repeat, i.e. it's being held down</param>
    /// <param name="unicode">The unicode value for key that was pressed</param>
    protected internal virtual void ReportKeyPressed(uint keyboardId, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        kEvs.Add(new(keyboardId, scancode, key, modifiers, true, repeat, unicode));
        kcEvs.Add(unicode);
    }

    internal void Clear()
    {
        WheelDelta = default;
        MousePosition = default;
        kEvs.Clear();
        kcEvs.Clear();
        mEvs.Clear();
    }

    internal void CopyTo(InputSnapshot other)
    {
        other.butt = butt;
        other.WheelDelta = WheelDelta;
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
