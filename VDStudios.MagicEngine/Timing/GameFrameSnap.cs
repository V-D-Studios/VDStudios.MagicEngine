using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Timing;

/// <summary>
/// Represents a snap of a given frame of <paramref name="Game"/>, to later present the difference between the current frame and this one
/// </summary>
/// <param name="Game"></param>
public readonly record struct GameFrameSnap(Game Game)
{
    private readonly ulong Start = Game.FrameCount;

    /// <summary>
    /// The current offset between <see cref="Game"/>'s current frame and the one snapped by this object
    /// </summary>
    public uint Offset => (uint)(Game.FrameCount - Start);

    /// <summary>
    /// <see langword="true"/> if this snap is not attached to a <see cref="Game"/>
    /// </summary>
    public bool IsDefault => Game is null;
}
