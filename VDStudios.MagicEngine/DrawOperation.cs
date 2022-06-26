using SDL2.NET;
using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn
/// </summary>
public abstract class DrawOperation : IDisposable
{
    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="renderer">The SDL2 renderer</param>
    protected internal abstract void Draw(Vector2 offset, Renderer renderer);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time
    /// </summary>
    protected internal abstract void Start();

#error Implement DrawOperation for Veldrid
}