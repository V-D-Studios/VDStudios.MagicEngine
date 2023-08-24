using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a timer that can be used to time events using a <see cref="GraphicsManager"/>'s frames as reference for time
/// </summary>
public readonly record struct GraphicsManagerFrameTimer(GraphicsManager GraphicsManager, uint Lapse)
{
    /// <summary>
    /// The amount of frames to wait before the timer is triggered
    /// </summary>
    /// <remarks>
    /// The timer will only be considered triggered if checked in the exact it is triggered, thus it needs to be checked every frame
    /// </remarks>
    public uint Lapse { get; } = Lapse + (uint)(GraphicsManager.FrameCount % Lapse);

    /// <summary>
    /// Whether or not this <see cref="GraphicsManagerFrameTimer"/> is triggered this frame
    /// </summary>
    /// <remarks>
    /// The timer will only be considered triggered if checked in the exact it is triggered, thus it needs to be checked every frame
    /// </remarks>
    public bool IsTriggered => GraphicsManager.FrameCount % Lapse == 0;
}
