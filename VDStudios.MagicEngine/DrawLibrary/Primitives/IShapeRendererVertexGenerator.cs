using System.Numerics;
using VDStudios.MagicEngine.Geometry;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents an object that generates vertices for a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
/// <typeparam name="TVertex">The type of the vertex the <see cref="ShapeRenderer{TVertex}"/> expects</typeparam>
public interface IShapeRendererVertexGenerator<TVertex> where TVertex : unmanaged
{
    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting a new <typeparamref name="TVertex"/> instance
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the VertexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="index">The current index of the vertex being requested (Has nothing to do with the index buffer)</param>
    /// <param name="shapeVertex">The actual point in the plane the generated vertex represents</param>
    /// <param name="shape">The shape the vertex is being generated for</param>
    /// <returns>The newly generated vertex</returns>
    public TVertex Generate(int index, Vector2 shapeVertex, ShapeDefinition shape);
}

/// <summary>
/// Represents an <see cref="IShapeRendererVertexGenerator{TVertex}"/> that returns the passed <see cref="Vector2"/> vertex of the shape as-is
/// </summary>
/// <remarks>
/// This is a singleton class, see <see cref="Default"/>
/// </remarks>
public sealed class ShapeVertexGenerator : IShapeRendererVertexGenerator<Vector2>
{
    /// <inheritdoc/>
    public Vector2 Generate(int index, Vector2 shapeVertex, ShapeDefinition shape) => shapeVertex;

    /// <summary>
    /// The default <see cref="ShapeVertexGenerator"/> for <see cref="ShapeRenderer"/> objects that don't need any aditional data for their vertices
    /// </summary>
    public static IShapeRendererVertexGenerator<Vector2> Default { get; } = new ShapeVertexGenerator();
}