using System.Numerics;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents an <see cref="IShapeRendererVertexGenerator{TVertex}"/> that returns the passed <see cref="Vector2"/> vertex of the shape as-is
/// </summary>
/// <remarks>
/// This is a singleton class, see <see cref="Default"/>
/// </remarks>
public sealed class ShapeVertexGenerator : IShapeRendererVertexGenerator<Vector2>
{
    /// <summary>
    /// The default <see cref="ShapeVertexGenerator"/> for <see cref="ShapeRenderer"/> objects that don't need any aditional data for their vertices
    /// </summary>
    public static IShapeRendererVertexGenerator<Vector2> Default { get; } = new ShapeVertexGenerator();

    /// <inheritdoc/>
    public void Start(ShapeRenderer<Vector2> renderer, IEnumerable<ShapeDefinition> allShapes, int regenCount, ref object? context) { }

    /// <inheritdoc/>
    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<Vector2> vertices, CommandList commandList, DeviceBuffer vertexBuffer, int index, out bool useDeviceBuffer, ref object? context)
    {
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = shape[i];
        useDeviceBuffer = false;
    }

    /// <inheritdoc/>
    public void Stop(ShapeRenderer<Vector2> renderer, ref object? context) { }
}