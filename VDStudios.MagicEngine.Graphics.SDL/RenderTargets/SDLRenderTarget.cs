namespace VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

/// <summary>
/// An SDL Render Target
/// </summary>
public abstract class SDLRenderTarget : RenderTarget<SDLGraphicsContext>
{
    /// <inheritdoc/>
    protected SDLRenderTarget(SDLGraphicsManager manager) : base(manager) { }

    /// <summary>
    /// The <see cref="DrawTransformation"/> for this <see cref="RenderTarget{TGraphicsContext}"/>
    /// </summary>
    public virtual DrawTransformation Transformation { get; protected set; }
}
