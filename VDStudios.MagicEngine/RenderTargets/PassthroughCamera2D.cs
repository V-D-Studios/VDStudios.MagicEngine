using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.RenderTargets;

/// <summary>
/// Represents a simple 2D Camera that passes the main <see cref="Framebuffer"/> from its <see cref="Owner"/> 
/// </summary>
public class PassthroughCamera2D : IRenderTarget
{
    private readonly DrawParameters drawParameters;

    /// <inheritdoc/>
    public GraphicsManager Owner { get; }

    /// <summary>
    /// Instances a new <see cref="PassthroughCamera2D"/> object
    /// </summary>
    /// <param name="owner">The <see cref="GraphicsManager"/> that owns this camera</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PassthroughCamera2D(GraphicsManager owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        drawParameters = new();
        Owner.RegisterSharedDrawResource(drawParameters);
    }

    /// <inheritdoc/>
    public void GetTarget(GraphicsDevice device, out Framebuffer targetBuffer, out DrawParameters targetParameters)
    {
        targetBuffer = device.SwapchainFramebuffer;
        targetParameters = drawParameters;
    }

    /// <inheritdoc/>
    public void CopyToScreen(CommandList managerCommandList, Framebuffer framebuffer, GraphicsDevice device) { }

    /// <inheritdoc/>
    public bool QueryCopyToScreenRequired(GraphicsDevice device)
        => false;

    /// <inheritdoc/>
    public void PrepareForDraw(CommandList managerCommandList) { }
}
