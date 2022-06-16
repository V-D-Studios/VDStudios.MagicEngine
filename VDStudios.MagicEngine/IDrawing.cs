using SDL2.NET;
using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> or <see cref="FunctionalComponent{TNode}"/> that is ready to be drawn
/// </summary>
public interface IDrawing
{
    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="renderer">The SDL2 renderer</param>
    public void Draw(Vector2 offset, Renderer renderer);
}