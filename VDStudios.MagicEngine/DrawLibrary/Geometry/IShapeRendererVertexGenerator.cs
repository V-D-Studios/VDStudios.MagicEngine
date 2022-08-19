using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents an object that generates vertices for a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
/// <typeparam name="TVertex">The type of the vertex the <see cref="ShapeRenderer{TVertex}"/> expects</typeparam>
public interface IShapeRendererVertexGenerator<TVertex> where TVertex : unmanaged
{
    /// <summary>
    /// Queries this generator to know if a CPU buffer should be allocated on the stack. If false, only the <see cref="DeviceBuffer"/> passed to <see cref="Generate(ShapeDefinition, IEnumerable{ShapeDefinition}, Span{TVertex}, CommandList, DeviceBuffer, out bool, ref object?)"/> will be usable, while the <see cref="Span{T}"/> will have a length of 0
    /// </summary>
    /// <param name="shape">The <see cref="ShapeDefinition"/> the vertices will be generated for</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    public bool QueryAllocCPUBuffer(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes) => true;

    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting new <typeparamref name="TVertex"/> data
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the VertexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="shape">The shape the vertex is being generated for</param>
    /// <param name="vertices">The span that will contain the vertices that are generated on the CPU. Will always be the same size as the amount of vertices in the shape, unless override by <see cref="QueryAllocCPUBuffer(ShapeDefinition, IEnumerable{ShapeDefinition})"/>. Ignore if the vertices will be written directly into <paramref name="vertexBuffer"/></param>
    /// <param name="vertexBuffer">The <see cref="DeviceBuffer"/> that contains the actual vertex data. Use it only if you intend to generate the vertices on the GPU</param>
    /// <param name="commandList">The command list in the context of the <see cref="ShapeRenderer{TVertex}"/> that owns <paramref name="shape"/></param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="useDeviceBuffer">Set to <c>true</c> if <paramref name="vertexBuffer"/> was filled in this method and should be used as-is, <c>false</c> if <paramref name="vertices"/> was filled instead and needs to be copied over</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single vertex regeneration</param>
    public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<TVertex> vertices, CommandList commandList, DeviceBuffer vertexBuffer, out bool useDeviceBuffer, ref object? context);
}
