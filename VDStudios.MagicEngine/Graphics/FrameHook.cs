namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an object that is used to hook in on every frame of a <see cref="GraphicsManager"/>
/// </summary>
/// <remarks>
/// This object is an abstraction, and the actual data is handled and seen entirely on <see cref="GraphicsManager{TGraphicsContext}"/>'s implementation of this <see cref="FrameHook"/>
/// </remarks>
public abstract class FrameHook : DisposableGameObject
{
    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> this <see cref="FrameHook"/> is watching
    /// </summary>
    protected readonly GraphicsManager Owner;

    /// <summary>
    /// Represents the amount of bits per pixel a Frame from <see cref="Owner"/> has
    /// </summary>
    public required uint BitsPerPixel { get; init; }

    /// <summary>
    /// Represents the amount of bytes per pixel a Frame from <see cref="Owner"/> has
    /// </summary>
    public required uint BytesPerPixel { get; init; }

    /// <summary>
    /// Creates a new object of type <see cref="FrameHook"/>
    /// </summary>
    protected FrameHook(GraphicsManager owner) : base(owner.Game, "Graphics & Input", "Video Recording")
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }
}
