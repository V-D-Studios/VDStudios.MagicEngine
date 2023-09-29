using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Timing;

/// <summary>
/// Represents a snap of a given frame of <paramref name="GraphicsManager"/>, to later present the difference between the current frame and this one
/// </summary>
/// <param name="GraphicsManager"></param>
public readonly record struct GraphicsManagerFrameSnap(GraphicsManager GraphicsManager)
{
    private readonly ulong Start = GraphicsManager.FrameCount;

    /// <summary>
    /// The current offset between <see cref="GraphicsManager"/>'s current frame and the one snapped by this object
    /// </summary>
    public uint Elapsed => (uint)(GraphicsManager.FrameCount - Start);
}
