using System.Numerics;

namespace VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

/// <summary>
/// Represents a render target that offers no transformation whatsoever to draw operations done through it
/// </summary>
public class PassthroughRenderTarget : SDLRenderTarget
{
    /// <inheritdoc/>
    public PassthroughRenderTarget(SDLGraphicsManager manager) : base(manager) { }

    /// <inheritdoc/>
    public override void BeginFrame(TimeSpan delta, SDLGraphicsContext context) 
    {
        Transformation = new DrawTransformation(Matrix4x4.Identity, context.Manager.WindowView);
    }

    /// <inheritdoc/>
    public override void RenderDrawOperation(TimeSpan delta, SDLGraphicsContext context, DrawOperation<SDLGraphicsContext> drawOperation)
    {
        InvokeDrawOperation(delta, drawOperation, context);
    }

    /// <inheritdoc/>
    public override void EndFrame(SDLGraphicsContext context) { }
}
