using System.Numerics;
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
    /// Queries this generator to know if a CPU buffer should be allocated on the stack. If false, only the <see cref="DeviceBuffer"/> passed to <see cref="Generate(ShapeDefinition, Span{TVertex}, CommandList, DeviceBuffer, out bool)"/> will be usable, while the <see cref="Span{T}"/> will have a length of 0
    /// </summary>
    /// <param name="shape">The <see cref="ShapeDefinition"/> the vertices will be generated for</param>
    public bool QueryAllocCPUBuffer(ShapeDefinition shape) => true;

    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting new <typeparamref name="TVertex"/> data
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the VertexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="shape">The shape the vertex is being generated for</param>
    /// <param name="vertices">The span that will contain the vertices that are generated on the CPU. Will usually be the same size as the amount of vertices in the shape, unless override by <see cref="QueryAllocCPUBuffer(ShapeDefinition)"/>. Ignore if the vertices will be written directly into <paramref name="vertexBuffer"/></param>
    /// <param name="vertexBuffer">The <see cref="DeviceBuffer"/> that contains the actual vertex data. Use it only if you intend to generate the vertices on the GPU</param>
    /// <param name="commandList">The command list in the context of the <see cref="ShapeRenderer{TVertex}"/> that owns <paramref name="shape"/></param>
    /// <param name="useDeviceBuffer">Set to <c>true</c> if <paramref name="vertexBuffer"/> was filled in this method and should be used as-is, <c>false</c> if <paramref name="vertices"/> was filled instead and needs to be copied over</param>
    public void Generate(ShapeDefinition shape, Span<TVertex> vertices, CommandList commandList, DeviceBuffer vertexBuffer, out bool useDeviceBuffer);
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
    public void Generate(ShapeDefinition shape, Span<Vector2> vertices, CommandList commandList, DeviceBuffer vertexBuffer, out bool useDeviceBuffer)
    {
        for (int i = 0; i < vertices.Length; i++) 
            vertices[i] = shape[i];
        useDeviceBuffer = false;
    }

    /// <summary>
    /// The default <see cref="ShapeVertexGenerator"/> for <see cref="ShapeRenderer"/> objects that don't need any aditional data for their vertices
    /// </summary>
    public static IShapeRendererVertexGenerator<Vector2> Default { get; } = new ShapeVertexGenerator();
}