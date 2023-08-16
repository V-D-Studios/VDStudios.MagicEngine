using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Input;

/// <summary>
/// Represents a snapshot of given input at a specific frame
/// </summary>
/// <remarks>
/// Input events get recorded at all times, and are reset at the beginning of a new frame. Disposing this object returns it to the Snapshot pool, not doing so may result in extra allocations
/// </remarks>
public class InputSnapshot
{
    internal InputSnapshot(InputSnapshotBuffer buffer)
    {
        Manager = buffer.Manager;
        Created = buffer.LastUpdated;
        CopyFrom(buffer);
    }

    /// <summary>
    /// Copies data from <paramref name="buffer"/> into this <see cref="InputSnapshot"/>
    /// </summary>
    /// <param name="buffer"></param>
    [MemberNotNull(nameof(KeyEvents), nameof(KeyCharPresses), nameof(MouseEvents), nameof(MouseWheelEvents), nameof(MousePosition), nameof(WheelDelta), nameof(PressedMouseButtons))]
    protected virtual void CopyFrom(InputSnapshotBuffer buffer)
    {
        KeyEvents = new List<KeyEventRecord>(buffer.KeyEvents);
        KeyCharPresses = new List<uint>(buffer.KeyCharPresses);
        MouseEvents = new List<MouseEventRecord>(buffer.MouseEvents);
        MouseWheelEvents = new List<MouseWheelEventRecord>(buffer.MouseWheelEvents);
        MousePosition = buffer.MousePosition;
        WheelDelta = buffer.WheelDelta;
        PressedMouseButtons = buffer.PressedMouseButtons;
    }

    /// <summary>
    /// The <see cref="GraphicsManager"/> that created this <see cref="InputSnapshot"/>
    /// </summary>
    public GraphicsManager Manager { get; }

    /// <summary>
    /// The system time at the moment of this <see cref="InputSnapshot"/>'s creation
    /// </summary>
    public DateTime Created { get; }

    /// <summary>
    /// The key events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<KeyEventRecord> KeyEvents { get; private set; }

    /// <summary>
    /// The characters that were pressed at the time of this snapshot
    /// </summary>
    public IReadOnlyList<uint> KeyCharPresses { get; private set; }

    /// <summary>
    /// The mouse events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<MouseEventRecord> MouseEvents { get; private set; }

    /// <summary>
    /// The mouse wheel events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<MouseWheelEventRecord> MouseWheelEvents { get; private set; }

    /// <summary>
    /// The mouse position at the time of this snapshot
    /// </summary>
    public Vector2 MousePosition { get; private set; }

    /// <summary>
    /// The mouse wheel's delta at the time of this snapshot
    /// </summary>
    public Vector2 WheelDelta { get; private set; }

    /// <summary>
    /// The mouse buttons that were pressed at the last moment of this snapshot
    /// </summary>
    public MouseButton PressedMouseButtons { get; private set; }

    /// <summary>
    /// Checks if <paramref name="button"/> was pressed at the time of this snapshot
    /// </summary>
    /// <param name="button"></param>
    public bool IsMouseDown(MouseButton button) => PressedMouseButtons.HasFlag(button);
}
