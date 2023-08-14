using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a Render Target for a GraphicsManager
/// </summary>
public interface IRenderTarget<TGraphicsContext> 
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> that owns this <see cref="IRenderTarget{TGraphicsContext}"/>
    /// </summary>
    public GraphicsManager<TGraphicsContext> Manager { get; }

    /// <summary>
    /// Signals this <see cref="IRenderTarget{TGraphicsContext}"/> to start a new frame to render into
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    public void BeginFrame(TGraphicsContext context);

    /// <summary>
    /// Renders <paramref name="drawOperation"/> into this target
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    /// <param name="drawOperation">The <see cref="DrawOperation{TGraphicsContext}"/> that is to be renderered into this target</param>
    public void RenderDrawOperation(TGraphicsContext context, DrawOperation<TGraphicsContext> drawOperation);

    /// <summary>
    /// Signals this <see cref="IRenderTarget{TGraphicsContext}"/> that the frame previously started by <see cref="BeginFrame(TGraphicsContext)"/> should be flushed
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    public void EndFrame(TGraphicsContext context);
}
