using System.Numerics;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary.Geometry;

/// <summary>
/// An index generator that merely outputs a sequence from 0 to the expected number of indices, respecting the set vertex skip if any
/// </summary>
public class Shape2DLinearIndexGenerator : IShape2DRendererIndexGenerator
{
    /// <inheritdoc/>
    public void Start(IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context) { }

    /// <inheritdoc/>
    public void GenerateUInt16(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, Span<ushort> indices, CommandList commandList, DeviceBuffer indexBuffer, int index, int indexCount, ElementSkip vertexSkip, uint indexStart, uint indexSize, out bool isBufferReady, ref object? context)
    {
        GenerateLineStripIndices((ushort)shape.Count, indices, (ushort)vertexSkip.GetSkipFactor(shape.Count));
        isBufferReady = false;
    }

    /// <inheritdoc/>
    public bool QueryUInt16BufferSize(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, int index, ElementSkip vertexSkip, out int indexCount, out int indexSpace, ref object? context)
    {
        indexCount = ComputeLineStripIndexBufferSize(vertexSkip.GetElementCount(shape.Count));
        indexSpace = ComputeLineStripIndexBufferSize(shape.Count);
        return true;
    }

    /// <inheritdoc/>
    public void Stop(ref object? context) { }

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a line strip
    /// </summary>
    /// <typeparam name="TInt">The type of integer to fill the buffer with</typeparam>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    public static void GenerateLineStripIndices<TInt>(TInt count, Span<TInt> indexBuffer, TInt step) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        for (TInt ind = TInt.Zero; ind < count; ind++)
            indexBuffer[int.CreateSaturating(ind)] = TInt.Clamp(step * ind, TInt.Zero, count);
        indexBuffer[int.CreateSaturating(count)] = TInt.Zero;
        return;
    }

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store a set of indices describing a line strip for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    public static int ComputeLineStripIndexBufferSize(int vertexCount)
        => vertexCount + 1;
}