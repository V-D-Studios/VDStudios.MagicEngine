using SDL2.NET;
using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be draw
/// </summary>
public interface IDrawOperation
{
    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="renderer">The SDL2 renderer</param>
    public void Draw(Vector2 offset, Renderer renderer);
}