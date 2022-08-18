using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// A VertexGenerator for <see cref="TextureVertex{TVertex}"/> data that intends to fill the image into each individual shape
/// </summary>
/// <typeparam name="TVertex"></typeparam>
public class TextureVertexGeneratorFill<TVertex> : IShapeRendererVertexGenerator<TextureVertex<TVertex>> where TVertex : unmanaged
{
    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<TextureVertex<TVertex>> vertices, CommandList commandList, DeviceBuffer vertexBuffer, out bool useDeviceBuffer)
    {
        throw new NotImplementedException();
    }

    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<TextureVertex<TVertex>> vertices)
    {
        throw new NotImplementedException();
    }
}
