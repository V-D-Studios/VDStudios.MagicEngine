using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a timer that can be used to time events using a <see cref="GraphicsManager"/>'s frames as reference for time
/// </summary>
public readonly record struct GraphicsManagerFrameTimer(GraphicsManager GraphicsManager, uint Lapse)
{
    private readonly ulong Start = GraphicsManager.FrameCount;

    /// <summary>
    /// The amount of frames to wait before the timer is triggered
    /// </summary>
    /// <remarks>
    /// The timer will only be considered triggered if checked in the exact it is triggered, thus it needs to be checked every frame
    /// </remarks>
    public uint Lapse { get; } = Lapse;

    /// <summary>
    /// The amount of times <see cref="Lapse"/> has elapsed
    /// </summary>
    public uint Clocks => (uint)((GraphicsManager.FrameCount - Start) / Lapse);

    /// <summary>
    /// Creates a new <see cref="GraphicsManagerFrameTimer"/> that contains the same parameters as this one, but begins counting from the moment this call completes
    /// </summary>
    /// <remarks>
    /// It's usually a good idea to replace the old GraphicsManagerFrameTimer with this one
    /// </remarks>
    public GraphicsManagerFrameTimer RestartNew()
        => new(GraphicsManager, Lapse);
}
