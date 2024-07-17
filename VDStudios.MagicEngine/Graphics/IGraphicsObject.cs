namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a context for a given <see cref="DrawOperation{TGraphicsContext}"/> or <see cref="RenderTarget{TGraphicsContext}"/>
/// </summary>
public interface IGraphicsObject<TGraphicsContext> where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> this operation is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this operation is not registered
    /// </remarks>
    GraphicsManager<TGraphicsContext>? Manager { get; }
}