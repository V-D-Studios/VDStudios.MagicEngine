using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Timing;

/// <summary>
/// Represents a timer that can be used to time events using a <see cref="Game"/>'s frames as reference for time
/// </summary>
public readonly record struct GameFrameTimer(Game Game, uint Lapse)
{
    private readonly ulong Start = Game.FrameCount;

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
    public uint Clocks => (uint)((Game.FrameCount - Start) / Lapse);

    /// <summary>
    /// Whether or not this timer is clocking at the current frame.
    /// </summary>
    /// <remarks>
    /// This property is only reliable when called from within the Update thread, that is, from within a Node, Scene, or similar. If expected to work outside this thread, consider using <see cref="HasClocked"/> instead
    /// </remarks>
    public bool IsClocking => (Game.FrameCount - Start) % Lapse == 0;

    /// <summary>
    /// Whether or not this <see cref="GraphicsManagerFrameTimer"/> has clocked at least once since it starting
    /// </summary>
    public bool HasClocked => Clocks > 0;

    /// <summary>
    /// Creates a new <see cref="GameFrameTimer"/> that contains the same parameters as this one, but begins counting from the moment this call completes
    /// </summary>
    /// <remarks>
    /// It's usually a good idea to replace the old GameFrameTimer with this one
    /// </remarks>
    public GameFrameTimer RestartNew()
        => new(Game, Lapse);

    /// <summary>
    /// <see langword="true"/> if this timer is not attached to a <see cref="Game"/>
    /// </summary>
    public bool IsDefault => Game is null;
}
