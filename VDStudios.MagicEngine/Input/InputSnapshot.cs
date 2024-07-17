using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using VDStudios.MagicEngine.Graphics;

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
    [MemberNotNull(nameof(KeyEvents), nameof(KeyCharPresses), nameof(KeyEventDictionary), nameof(MouseEvents), nameof(MouseWheelEvents), nameof(MousePosition), nameof(WheelDelta), nameof(PressedMouseButtons), nameof(TextInputEvents))]
    protected virtual void CopyFrom(InputSnapshotBuffer buffer)
    {
        TextInputEvents = new List<TextInputEventRecord>(buffer.TextInputEvents);
        KeyEvents = new List<KeyEventRecord>(buffer.KeyEvents);
        KeyCharPresses = new List<uint>(buffer.KeyCharPresses);
        MouseEvents = new List<MouseEventRecord>(buffer.MouseEvents);
        MouseWheelEvents = new List<MouseWheelEventRecord>(buffer.MouseWheelEvents);
        MousePosition = buffer.MousePosition;
        WheelDelta = buffer.WheelDelta;
        KeyEventDictionary = new Dictionary<Scancode, KeyPressRecord>(buffer.KeyEventDictionary);
        PressedMouseButtons = buffer.PressedMouseButtons;
        ActiveModifiers = buffer.ActiveModifiers;
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
    /// A dictionary relating Key Events to their respective scancode
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="KeyEvents"/>, this only maintains the latest state of the key. This property will not reflect if, for example, the key was pressed multiple times in a single frame
    /// </remarks>
    public IReadOnlyDictionary<Scancode, KeyPressRecord> KeyEventDictionary { get; private set; }

    /// <summary>
    /// The active <see cref="KeyModifier"/>s by the end of the frame
    /// </summary>
    public KeyModifier ActiveModifiers { get; private set; }

    /// <summary>
    /// The text input events that happened at the time of this snapshot
    /// </summary>
    public IReadOnlyList<TextInputEventRecord> TextInputEvents { get; private set; }

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
