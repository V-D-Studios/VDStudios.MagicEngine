namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an object that is used to hook in on every frame of a <see cref="GraphicsManager"/>
/// </summary>
/// <remarks>
/// This object is an abstraction, and the actual data is handled and seen entirely on <see cref="GraphicsManager{TGraphicsContext}"/>'s implementation of this <see cref="FrameHook"/>
/// </remarks>
public abstract class FrameHook : IDisposable
{
    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> this <see cref="FrameHook"/> is watching
    /// </summary>
    protected readonly GraphicsManager Owner;

    /// <summary>
    /// Creates a new object of type <see cref="FrameHook"/>
    /// </summary>
    protected FrameHook(GraphicsManager owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// Disposes of this <see cref="FrameHook"/>
    /// </summary>
    /// <remarks>
    /// Disconnects this <see cref="FrameHook"/> from its owner <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </remarks>
    /// <param name="disposing"></param>
    protected abstract void Dispose(bool disposing);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
