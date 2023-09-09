using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.RenderTargets;

/// <summary>
/// A render target that passes operations through directly
/// </summary>
public class PassthroughRenderTarget : VeldridRenderTarget
{
    /// <inheritdoc/>
    public PassthroughRenderTarget(VeldridGraphicsManager manager) : base(manager) { }

    /// <inheritdoc/>
    public override Framebuffer GetFramebuffer(VeldridGraphicsContext context)
        => context.GraphicsDevice.SwapchainFramebuffer;

    /// <inheritdoc/>
    public override void RenderDrawOperation(TimeSpan delta, VeldridGraphicsContext context, DrawOperation<VeldridGraphicsContext> drawOperation)
    {
        InvokeDrawOperation(delta, drawOperation, context);
    }

    /// <inheritdoc/>
    public override void EndFrame(VeldridGraphicsContext context) { }
}
