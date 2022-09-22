using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents an object that generates indices for a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
public interface IShape2DRendererIndexGenerator
{
    /// <summary>
    /// Queries this generator to know if a CPU buffer should be allocated on the stack. If <see langword="false"/>, only the <see cref="DeviceBuffer"/> passed to <see cref="GenerateUInt16(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, Span{ushort}, CommandList, DeviceBuffer, int, ElementSkip, uint, uint, out bool, ref object?)"/> will be usable, while the <see cref="Span{T}"/> will have a length of 0
    /// </summary>
    /// <param name="shape">The <see cref="ShapeDefinition2D"/> the vertices will be generated for</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    public bool QueryAllocCPUBufferUInt16(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, ref object? context) => true;

    /// <summary>
    /// This method is called when the generator is about to be used for a index regeneration batch, and can be used to prepare a context for a given Renderer
    /// </summary>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="regenCount">The amount of shapes that will require their vertices to be regenerated</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    public void Start(IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context);

    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting new index <see cref="ushort"/> data
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the IndexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="shape">The shape the index is being generated for</param>
    /// <param name="index">The index of the shape whose vertices are being generated in relation to the amount of shapes that will be regenerated in this batch</param>
    /// <param name="vertices">The span that will contain the vertices that are generated on the CPU. Will always be the same size as the amount of vertices in the shape, unless <see cref="QueryAllocCPUBufferUInt16(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, ref object?)"/> returned <see langword="false"/>. Ignore if the vertices will be written directly into <paramref name="indexBuffer"/></param>
    /// <param name="indexBuffer">The <see cref="DeviceBuffer"/> that contains the actual index data. Use it only if you intend to generate the vertices on the GPU</param>
    /// <param name="commandList">The command list in the context of the <see cref="ShapeRenderer{TVertex}"/> that owns <paramref name="shape"/></param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="useDeviceBuffer">Set to <c>true</c> if <paramref name="indexBuffer"/> was filled in this method and should be used as-is, <c><see langword="false"/></c> if <paramref name="vertices"/> was filled instead and needs to be copied over</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    /// <param name="indexSize">The amount of space *in bytes* allocated for the indices in <paramref name="indexBuffer"/>. Writing more bytes than allocated, or after the byte resulting from <paramref name="indexStart"/> + <paramref name="indexSize"/> will most likely result in corrupted vertex data, or an exception being thrown.</param>
    /// <param name="indexStart">The byte where the space allocated for the indices start. Writing before this byte, or after the byte resulting from <paramref name="indexStart"/> + <paramref name="indexSize"/> will most likely result in corrupted vertex data, or an exception being thrown.</param>
    /// <param name="vertexSkip">A struct that provides information on how to spread the indices that should be written to the index buffer</param>
    public void GenerateUInt16(
        ShapeDefinition2D shape, 
        IEnumerable<ShapeDefinition2D> allShapes, 
        Span<ushort> vertices, 
        CommandList commandList, 
        DeviceBuffer indexBuffer, 
        int index, 
        ElementSkip vertexSkip, 
        uint indexStart, 
        uint indexSize, 
        out bool useDeviceBuffer, 
        ref object? context
    );

    /// <summary>
    /// Queries this generator to know how many elements the index buffer is expected to contain
    /// </summary>
    /// <param name="shape">The shape the index is being generated for</param>
    /// <param name="index">The index of the shape whose vertices are being generated in relation to the amount of shapes that will be regenerated in this batch</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="vertexSkip">A struct that provides information on how to spread the indices that should be written to the index buffer</param>
    /// <returns></returns>
    public uint QueryUInt16BufferSize(
        ShapeDefinition2D shape,
        IEnumerable<ShapeDefinition2D> allShapes,
        int index,
        ElementSkip vertexSkip,
        ref object? context
    );

    /// <summary>
    /// This method is called when the generator finishes a index regeneration batch
    /// </summary>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    public void Stop(ref object? context);
}
