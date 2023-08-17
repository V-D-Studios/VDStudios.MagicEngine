using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// Represents a context for SDL Graphics
/// </summary>
public class SDLGraphicsContext : GraphicsContext<SDLGraphicsContext>
{
    /// <inheritdoc/>
    public SDLGraphicsContext(GraphicsManager<SDLGraphicsContext> manager) : base(manager) { }
}
