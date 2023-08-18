using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
#warning Consider generalizing and making this into simply IDrawable. Releasing it from its runtime (Not compile-time enforced) restriction to being a node
// This would require handling IDrawableNodes differently in scenes, but not necessarily treating nodes differently
public interface IDrawableNode<TGraphicsContext> : IGameObject
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Represents the class that manages this <see cref="IDrawableNode{TGraphicsContext}"/>'s <see cref="DrawOperation{TGraphicsContext}"/>s
    /// </summary>
    public DrawOperationManager<TGraphicsContext> DrawOperationManager { get; }
}
