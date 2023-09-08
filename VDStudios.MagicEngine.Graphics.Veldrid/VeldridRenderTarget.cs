using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <inheritdoc/>
public abstract class VeldridRenderTarget : RenderTarget<VeldridGraphicsContext>
{
    /// <summary>
    /// Creates a new <see cref="VeldridRenderTarget"/> owned by <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The manager that owns this <see cref="VeldridRenderTarget"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected VeldridRenderTarget(VeldridGraphicsManager manager) : base(manager) { }

    internal CommandList? cl;

    /// <summary>
    /// The command list for this <see cref="VeldridRenderTarget"/>
    /// </summary>
    public CommandList CommandList => cl ?? throw new InvalidOperationException("Cannot obtain a CommandList for this RenderTarget before a Frame Starts or after a Frame ends");
}
