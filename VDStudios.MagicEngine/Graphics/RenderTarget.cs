namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a Render Target for a GraphicsManager
/// </summary>
public abstract class RenderTarget<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Creates a new <see cref="RenderTarget{TGraphicsContext}"/> owned by <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The manager that owns this <see cref="RenderTarget{TGraphicsContext}"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected RenderTarget(GraphicsManager<TGraphicsContext> manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// The <see cref="DrawTransformation"/> for this <see cref="RenderTarget{TGraphicsContext}"/>
    /// </summary>
    public virtual DrawTransformation Transformation { get; protected set; }

    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> that owns this <see cref="RenderTarget{TGraphicsContext}"/>
    /// </summary>
    public GraphicsManager<TGraphicsContext> Manager { get; }

    /// <summary>
    /// Signals this <see cref="RenderTarget{TGraphicsContext}"/> to start a new frame to render into
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    /// <param name="delta">The amount of time it took to render the last frame</param>
    public abstract void BeginFrame(TimeSpan delta, TGraphicsContext context);

    /// <summary>
    /// Renders <paramref name="drawOperation"/> into this target
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    /// <param name="drawOperation">The <see cref="DrawOperation{TGraphicsContext}"/> that is to be renderered into this target</param>
    /// <param name="delta">The amount of time it took to render the last frame</param>
    public abstract void RenderDrawOperation(TimeSpan delta, TGraphicsContext context, DrawOperation<TGraphicsContext> drawOperation);

    /// <summary>
    /// Signals this <see cref="RenderTarget{TGraphicsContext}"/> that the frame previously started by <see cref="BeginFrame(TimeSpan, TGraphicsContext)"/> should be flushed
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> that contains the info for this target</param>
    public abstract void EndFrame(TGraphicsContext context);

    /// <summary>
    /// Invokes <paramref name="drawOperation"/>'s drawing methods
    /// </summary>
    /// <param name="drawOperation">The <see cref="DrawOperation{TGraphicsContext}"/> to invoke</param>
    /// <param name="context">The context to pass to <see cref="DrawOperation{TGraphicsContext}"/></param>
    /// <param name="delta">The amount of time it took to render the last frame</param>
    protected void InvokeDrawOperation(TimeSpan delta, DrawOperation<TGraphicsContext> drawOperation, TGraphicsContext context)
        => drawOperation.InternalDraw(delta, context, this);
}
