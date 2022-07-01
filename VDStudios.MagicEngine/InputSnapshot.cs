using SDL2.NET;
using SDL2.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static SDL2.NET.Window;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a snapshot of given input at a specific time
/// </summary>
/// <remarks>
/// Disposing this object returns it to the Snapshot pool, not doing so may result in extra allocations
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
    public IReadOnlyList<KeyState> KeyEvents => kEvs;
    internal List<KeyState> kEvs = new();

    /// <summary>
    /// The characters that were pressed at the time of this snapshot
    /// </summary>
    public IReadOnlyList<char> KeyCharPresses => kcEvs;
    internal List<char> kcEvs = new();

    /// <summary>
    /// The mouse position at the time of this snapshot
    /// </summary>
    public Vector2 MousePosition { get; internal set; }

    /// <summary>
    /// The mouse wheel's delta at the time of this snapshot
    /// </summary>
    public float WheelDelta { get; internal set; }

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

    internal void Clear()
    {
        butt = 0;
        WheelDelta = 0;
        MousePosition = default;
        kEvs.Clear();
        kcEvs.Clear();
    }

    void IDisposable.Dispose() => ReturnToPool();
}
