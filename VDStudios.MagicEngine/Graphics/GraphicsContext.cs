namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a context for a given <see cref="DrawOperation{TGraphicsContext}"/> or <see cref="IRenderTarget{TGraphicsContext}"/>
/// </summary>
public abstract class GraphicsContext<TSelf>
    where TSelf : GraphicsContext<TSelf>
{
    /// <summary>
    /// The manager of this <see cref="GraphicsContext{TSelf}"/>
    /// </summary>
    public GraphicsManager<TSelf> Manager { get; }

    /// <summary>
    /// Creates a new object of type <see cref="GraphicsContext{TSelf}"/> with a given manager
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager{TGraphicsContext}"/> that owns this <see cref="GraphicsContext{TSelf}"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    public GraphicsContext(GraphicsManager<TSelf> manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }
}
