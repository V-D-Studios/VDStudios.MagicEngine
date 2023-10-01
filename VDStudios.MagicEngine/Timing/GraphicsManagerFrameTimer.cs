using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Timing;

/// <summary>
/// Represents a timer that can be used to time events using a <see cref="GraphicsManager"/>'s frames as reference for time
/// </summary>
public readonly record struct GraphicsManagerFrameTimer(GraphicsManager GraphicsManager, uint Lapse)
{
    private readonly ulong Start = GraphicsManager.FrameCount;

    /// <summary>
    /// Creates a new <see cref="GraphicsManagerFrameTimer"/> that is a copy of <paramref name="copy"/> with a new lapse
    /// </summary>
    /// <remarks>
    /// Useful for changing the lapse of a <see cref="GraphicsManagerFrameTimer"/> without changing its starting point, so as to not lose unnecessary clocks
    /// </remarks>
    public GraphicsManagerFrameTimer(GraphicsManagerFrameTimer copy, uint lapse)
        : this(copy.GraphicsManager, lapse)
    {
        Start = copy.Start;
    }

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
    /// <remarks>
    /// If <see cref="Lapse"/> is <c>0</c>, this will always return <c>1</c>
    /// </remarks>
    public uint Clocks => Lapse == 0 ? 1 : (uint)((GraphicsManager.FrameCount - Start) / Lapse);

    /// <summary>
    /// Whether or not this timer is clocking at the current frame.
    /// </summary>
    /// <remarks>
    /// This property is only reliable when called from within <see cref="GraphicsManager"/>'s rendering thread, that is, from within a <see cref="DrawOperation{TGraphicsContext}"/> belonging to this <see cref="GraphicsManager"/> or similar. If expected to work outside this thread, consider using <see cref="HasClocked"/> instead. Will always return <see langword="true"/> if <see cref="Lapse"/> is <c>0</c>
    /// </remarks>
    public bool IsClocking => Lapse == 0 || (GraphicsManager.FrameCount - Start) % Lapse == 0;

    /// <summary>
    /// Whether or not this <see cref="GraphicsManagerFrameTimer"/> has clocked at least once since it starting
    /// </summary>
    public bool HasClocked => Clocks > 0;

    /// <summary>
    /// <see langword="true"/> if this timer is not attached to a <see cref="GraphicsManager"/>
    /// </summary>
    public bool IsDefault => GraphicsManager is null;

    /// <summary>
    /// Creates a new <see cref="GraphicsManagerFrameTimer"/> that contains the same parameters as this one, but begins counting from the moment this call completes
    /// </summary>
    /// <remarks>
    /// It's usually a good idea to replace the old GraphicsManagerFrameTimer with this one
    /// </remarks>
    public GraphicsManagerFrameTimer RestartNew()
        => new(GraphicsManager, Lapse);
}
