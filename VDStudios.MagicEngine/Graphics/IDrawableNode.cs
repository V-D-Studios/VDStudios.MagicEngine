namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
public interface IDrawableNode<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Represents the class that manages this <see cref="IDrawableNode{TGraphicsContext}"/>'s <see cref="DrawOperation{TGraphicsContext}"/>s
    /// </summary>
    public DrawOperationManager<TGraphicsContext> DrawOperationManager { get; }

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }
}
