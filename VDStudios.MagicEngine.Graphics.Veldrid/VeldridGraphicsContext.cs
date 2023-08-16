using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a context for Veldrid
/// </summary>
public class VeldridGraphicsContext : GraphicsContext<VeldridGraphicsContext>
{
    /// <inheritdoc/>
    public VeldridGraphicsContext(GraphicsManager<VeldridGraphicsContext> manager, GraphicsDevice device) : base(manager) 
    {
        GraphicsDevice = device;
    }

    /// <summary>
    /// The <see cref="GraphicsDevice"/> for this context
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }
}
