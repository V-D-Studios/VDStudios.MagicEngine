using System.Numerics;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// A VertexGenerator for <see cref="TextureVertex{TVertex}"/> data that intends to fill the image into each individual shape
/// </summary>
public class TextureVertexGeneratorFill : IShapeRendererVertexGenerator<TextureVertex<Vector2>> 
{
    /// <inheritdoc/>
    public void Start(ShapeRenderer<TextureVertex<Vector2>> renderer, IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context) { }

    /// <inheritdoc/>
    public void Generate(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, Span<TextureVertex<Vector2>> vertices, CommandList commandList, DeviceBuffer vertexBuffer, int index, out bool useDeviceBuffer, ref object? context)
    {
        Vector2 distant = default;
        for (int i = 0; i < vertices.Length; i++)
            distant = Vector2.Max(distant, Vector2.Abs(shape[i]));
        Matrix3x2 trans = Matrix3x2.CreateScale(1 / distant.X, 1 / distant.Y);

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new(Vector2.Transform(shape[vertices.Length - 1 - i], trans), shape[i]);

        useDeviceBuffer = false;
    }

    /// <inheritdoc/>
    public void Stop(ShapeRenderer<TextureVertex<Vector2>> renderer, ref object? context) { }
}
