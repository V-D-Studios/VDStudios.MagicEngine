using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Internal;
using Veldrid;

namespace VDStudios.MagicEngine.RenderTargets;

/// <summary>
/// Represents a simple 2D Camera that passes the main <see cref="Framebuffer"/> from its <see cref="Camera2D.Owner"/> 
/// </summary>
public class PassthroughCamera2D : Camera2D
{
    /// <summary>
    /// Instances a new <see cref="PassthroughCamera2D"/> object
    /// </summary>
    /// <param name="owner">The <see cref="GraphicsManager"/> that owns this camera</param>
    /// <param name="interpolator">An <see cref="IInterpolator"/> object that interpolates the projection between its current state and its destined state</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PassthroughCamera2D(GraphicsManager owner, IInterpolator? interpolator) : base(owner, interpolator) { }

    /// <inheritdoc/>
    public override void GetTarget(GraphicsDevice device, TimeSpan delta, out Framebuffer targetBuffer, out DrawParameters targetParameters)
    {
        UpdateProjection(delta);
        targetBuffer = device.SwapchainFramebuffer;
        targetParameters = drawParameters;
        targetParameters.Transformation = new DrawTransformation(Owner.WindowView, Matrix4x4.Identity);
    }

    /// <inheritdoc/>
    public override void CopyToScreen(CommandList managerCommandList, Framebuffer framebuffer, GraphicsDevice device) { }

    /// <inheritdoc/>
    public override bool QueryCopyToScreenRequired(GraphicsDevice device)
        => false;

    /// <inheritdoc/>
    public override void PrepareForDraw(CommandList managerCommandList) { }
}
