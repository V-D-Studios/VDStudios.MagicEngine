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
    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<TextureVertex<Vector2>> vertices, CommandList commandList, DeviceBuffer vertexBuffer, out bool useDeviceBuffer, ref object? context)
    {


        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new();
    }
}
