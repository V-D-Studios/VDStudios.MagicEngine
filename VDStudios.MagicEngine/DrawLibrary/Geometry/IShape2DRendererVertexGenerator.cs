using SDL2.NET;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents an object that generates vertices for a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
/// <typeparam name="TVertex">The type of the vertex the <see cref="ShapeRenderer{TVertex}"/> expects</typeparam>
public interface IShape2DRendererVertexGenerator<TVertex> where TVertex : unmanaged
{
    /// <summary>
    /// Queries this generator to know if a CPU buffer should be allocated on the stack. If false, only the <see cref="DeviceBuffer"/> passed to <see cref="Generate(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, Span{TVertex}, CommandList, DeviceBuffer, int, uint, uint, out bool, ref object?)"/> will be usable, while the <see cref="Span{T}"/> will have a length of 0
    /// </summary>
    /// <param name="shape">The <see cref="ShapeDefinition2D"/> the vertices will be generated for</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single vertex regeneration</param>
    public bool QueryAllocCPUBuffer(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, ref object? context) => true;

    /// <summary>
    /// This method is called when the generator is about to be used for a vertex regeneration batch, and can be used to prepare a context for a given Renderer
    /// </summary>
    /// <param name="renderer">The renderer requiring new vertices to be generated</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="regenCount">The amount of shapes that will require their vertices to be regenerated</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single vertex regeneration</param>
    public void Start(ShapeRenderer<TVertex> renderer, IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context);

    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting new <typeparamref name="TVertex"/> data
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the VertexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="shape">The shape the vertex is being generated for</param>
    /// <param name="index">The index of the shape whose vertices are being generated in relation to the amount of shapes that will be regenerated in this batch</param>
    /// <param name="vertices">The span that will contain the vertices that are generated on the CPU. Will always be the same size as the amount of vertices in the shape, unless <see cref="QueryAllocCPUBuffer(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, ref object?)"/> returned false. Ignore if the vertices will be written directly into <paramref name="vertexBuffer"/></param>
    /// <param name="vertexBuffer">The <see cref="DeviceBuffer"/> that contains the actual vertex data. Use it only if you intend to generate the vertices on the GPU</param>
    /// <param name="commandList">The command list in the context of the <see cref="ShapeRenderer{TVertex}"/> that owns <paramref name="shape"/></param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="useDeviceBuffer">Set to <c>true</c> if <paramref name="vertexBuffer"/> was filled in this method and should be used as-is, <c>false</c> if <paramref name="vertices"/> was filled instead and needs to be copied over</param>
    /// <param name="vertexSize">The amount of space *in bytes* allocated for the vertices in <paramref name="vertexBuffer"/>. Writing more bytes than allocated, or after the byte resulting from <paramref name="vertexStart"/> + <paramref name="vertexSize"/> will most likely result in corrupted index data, or an exception being thrown.</param>
    /// <param name="vertexStart">The byte where the space allocated for the vertex start. Writing before this byte, or after the byte resulting from <paramref name="vertexStart"/> + <paramref name="vertexSize"/> will most likely result in corrupted index data, or an exception being thrown.</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single vertex regeneration</param>
    public void Generate(
        ShapeDefinition2D shape, 
        IEnumerable<ShapeDefinition2D> allShapes, 
        Span<TVertex> vertices, 
        CommandList commandList, 
        DeviceBuffer vertexBuffer, 
        int index,
        uint vertexStart,
        uint vertexSize,
        out bool useDeviceBuffer, 
        ref object? context
    );

    /// <summary>
    /// This method is called when the generator finishes a vertex regeneration batch
    /// </summary>
    /// <param name="renderer">The renderer that required the vertices to be generated</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single vertex regeneration</param>
    public void Stop(ShapeRenderer<TVertex> renderer, ref object? context);
}
