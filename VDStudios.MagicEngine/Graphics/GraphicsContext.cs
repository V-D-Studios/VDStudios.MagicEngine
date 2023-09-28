namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a context for a given <see cref="DrawOperation{TGraphicsContext}"/> or <see cref="RenderTarget{TGraphicsContext}"/>
/// </summary>
public abstract class GraphicsContext<TSelf> : GameObject
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
    public GraphicsContext(GraphicsManager<TSelf> manager) : base(manager.Game, "Graphics Context", "Rendering")
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Updates this GraphicsContext
    /// </summary>
    /// <param name="delta">The amount of time it took the last frame to render</param>
    public abstract void Update(TimeSpan delta);

    /// <summary>
    /// Notifies this <see cref="GraphicsContext{TSelf}"/> that a Frame is about to begin
    /// </summary>
    public abstract void BeginFrame();

    /// <summary>
    /// Notifies this <see cref="GraphicsContext{TSelf}"/> that the previously begun frame is ended and should be submitted
    /// </summary>
    public abstract void EndAndSubmitFrame();
}
