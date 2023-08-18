namespace VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

/// <summary>
/// An SDL Render Target
/// </summary>
public abstract class SDLRenderTarget : RenderTarget<SDLGraphicsContext>
{
    /// <inheritdoc/>
    protected SDLRenderTarget(SDLGraphicsManager manager) : base(manager) { }
}
