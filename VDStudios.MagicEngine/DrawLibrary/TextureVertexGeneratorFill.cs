using SDL2.NET.Input;
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
    public void Start(ShapeRenderer<TextureVertex<Vector2>> renderer, IEnumerable<ShapeDefinition> allShapes, int regenCount, ref object? context) { }

    /// <inheritdoc/>
    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<TextureVertex<Vector2>> vertices, CommandList commandList, DeviceBuffer vertexBuffer, int index, out bool useDeviceBuffer, ref object? context)
    {
        Vector2 distant = default;
        Vector2 offset = default;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (shape[i].Length() > distant.Length())
                distant = Vector2.Abs(shape[i]);
            if (shape[i].X < 0 && shape[i].X < offset.X)
                offset.X = shape[i].X;
            if (shape[i].Y < 0 && shape[i].Y < offset.Y)
                offset.Y = shape[i].Y;
        }
        Matrix3x2 trans = Matrix3x2.CreateScale(1 / distant.X, 1 / distant.Y);

        for (int i = vertices.Length - 1; i >= 0; --i)
            vertices[i] = new(Vector2.Transform(shape[i] , trans), shape[i]);

        useDeviceBuffer = false;
    }

    /// <inheritdoc/>
    public void Stop(ShapeRenderer<TextureVertex<Vector2>> renderer, ref object? context) { }
}
