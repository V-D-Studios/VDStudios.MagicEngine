using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary.Geometry;

/// <summary>
/// Represents an object that generates indices for a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
public interface IShape2DRendererIndexGenerator
{
    /// <summary>
    /// This method is called when the generator is about to be used for a index regeneration batch, and can be used to prepare a context for a given Renderer
    /// </summary>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="regenCount">The amount of shapes that will require their indices to be regenerated</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    public void Start(IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context);

    /// <summary>
    /// This method is called when the owning <see cref="ShapeRenderer{TVertex}"/> is requesting new index <see cref="ushort"/> data
    /// </summary>
    /// <remarks>
    /// Most commonly, this means the IndexBuffer for a given shape is being regenerated. No heavy work should be done here -- Let transformations and such happen in the GPU through shaders
    /// </remarks>
    /// <param name="shape">The shape the index is being generated for</param>
    /// <param name="index">The index of the shape whose indices are being generated in relation to the amount of shapes that will be regenerated in this batch</param>
    /// <param name="indices">The span that will contain the indices that are generated on the CPU. Will always be the same size as the amount of indices in the shape, unless <see cref="QueryUInt16BufferSize(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, int, ElementSkip, out int, out int, ref object?)"/> returned <see langword="false"/>. Ignore if the indices will be written directly into <paramref name="indexBuffer"/></param>
    /// <param name="indexBuffer">The <see cref="DeviceBuffer"/> that contains the actual index data. Use it only if you intend to generate the indices on the GPU</param>
    /// <param name="commandList">The command list in the context of the <see cref="ShapeRenderer{TVertex}"/> that owns <paramref name="shape"/></param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="isBufferReady">Set to <c>true</c> if <paramref name="indexBuffer"/> was filled in this method and should be used as-is, <c><see langword="false"/></c> if <paramref name="indices"/> was filled instead and needs to be copied over</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    /// <param name="indexSize">The amount of space *in bytes* allocated for the indices in <paramref name="indexBuffer"/>. Writing more bytes than allocated, or after the byte resulting from <paramref name="indexStart"/> + <paramref name="indexSize"/> will most likely result in corrupted vertex data, or an exception being thrown.</param>
    /// <param name="indexStart">The byte where the space allocated for the indices start. Writing before this byte, or after the byte resulting from <paramref name="indexStart"/> + <paramref name="indexSize"/> will most likely result in corrupted vertex data, or an exception being thrown.</param>
    /// <param name="vertexSkip">A struct that provides information on how to spread the indices that should be written to the index buffer</param>
    /// <param name="indexCount">The amount of indices that are going to be written, previously obtained from <see cref="QueryUInt16BufferSize(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, int, ElementSkip, out int, out int, ref object?)"/></param>
    /// <returns>The amount of indices that were generated</returns>
    public void GenerateUInt16(
        ShapeDefinition2D shape,
        IEnumerable<ShapeDefinition2D> allShapes,
        Span<ushort> indices,
        CommandList commandList,
        DeviceBuffer indexBuffer,
        int index,
        int indexCount,
        ElementSkip vertexSkip,
        uint indexStart,
        uint indexSize,
        out bool isBufferReady,
        ref object? context
    );

    /// <summary>
    /// Queries this generator to know how many elements the index buffer is expected to contain
    /// </summary>
    /// <param name="shape">The shape the index is being generated for</param>
    /// <param name="index">The index of the shape whose indices are being generated in relation to the amount of shapes that will be regenerated in this batch</param>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    /// <param name="allShapes">All the shapes that are currently owned by the <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="vertexSkip">A struct that provides information on how to spread the indices that should be written to the index buffer</param>
    /// <param name="indexCount">The amount of indices that are expected to be generated</param>
    /// <param name="indexSpace">The amount of indices the <see cref="DeviceBuffer"/> should allocate for. This can be used to have the index buffer be large enough to hold all the indices that would be generated if no indices are skipped, reducing extra allocations if <paramref name="vertexSkip"/> varies</param>
    /// <returns>
    /// If <see langword="false"/>, only the <see cref="DeviceBuffer"/> passed to <see cref="GenerateUInt16(ShapeDefinition2D, IEnumerable{ShapeDefinition2D}, Span{ushort}, CommandList, DeviceBuffer, int, ElementSkip, uint, uint, out bool, ref object?)"/> will be usable, while the <see cref="Span{T}"/> will have a length of 0
    /// </returns>
    public bool QueryUInt16BufferSize(
        ShapeDefinition2D shape,
        IEnumerable<ShapeDefinition2D> allShapes,
        int index,
        ElementSkip vertexSkip,
        out int indexCount,
        out int indexSpace,
        ref object? context
    );

    /// <summary>
    /// This method is called when the generator finishes a index regeneration batch
    /// </summary>
    /// <param name="context">An optional context parameter. The same reference will be used for all the calls relating to a <see cref="ShapeRenderer{TVertex}"/> in a single index regeneration</param>
    public void Stop(ref object? context);
}
