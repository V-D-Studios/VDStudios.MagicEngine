using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.MetalBindings;
using Veldrid;
using Vulkan;
using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Provides a variety of helper methods to manage GPU related vertices, buffer indices, etc
/// </summary>
public static class GPUHelpers
{
    /// <summary>
    /// The maximum amount of vertices a model can have when using 16 bit indices
    /// </summary>
    public const int MaxUInt16Vertices = 21845;

    private static void ThrowIfTooLarge(int count)
    {
        if (count >= MaxUInt16Vertices)
            throw new NotSupportedException($"Triangulating indices for shapes with {MaxUInt16Vertices} or more vertices is not supported! The shape in question has {count} vertices. The ints used for indices are 16 bits wide, and switching to 32 bits is not supported yet");
    }

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store triangulated indices for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <param name="added">A number, either a <c>1</c> or <c>0</c>, that offsets the triangulation to account for odd vertex counts</param>
    public static int ComputeConvexTriangulatedIndexBufferSize(int vertexCount, out byte added)
        => (vertexCount - (added = vertexCount % 2 == 0 ? (byte)0u : (byte)1u)) * 3;

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store a set of indices describing a line strip for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    public static int ComputeLineStripIndexBufferSize(int vertexCount)
        => vertexCount + 1;

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a triangle strip
    /// </summary>
    /// <typeparam name="TInt">The type of integer to fill the buffer with</typeparam>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    /// <param name="start">The starting point of the triangulation. Synonym to <c>added</c> in <see cref="ComputeConvexTriangulatedIndexBufferSize(int, out byte)"/></param>
    public static void GenerateConvexTriangulatedIndices<TInt>(TInt count, Span<TInt> indexBuffer, byte start = 0) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        TInt i = TInt.CreateSaturating(start);
        TInt p0 = TInt.Zero;
        TInt pHelper = TInt.One;
        TInt pTemp;

        for (; i < count; i++)
        {
            pTemp = i;
            indexBuffer[bufind++] = p0;
            indexBuffer[bufind++] = pHelper;
            indexBuffer[bufind++] = pTemp;
            pHelper = pTemp;
        }
    }

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a line strip
    /// </summary>
    /// <typeparam name="TInt">The type of integer to fill the buffer with</typeparam>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    public static void GenerateLineStripIndices<TInt>(TInt count, Span<TInt> indexBuffer) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        for (TInt ind = TInt.Zero; ind < count; ind++)
            indexBuffer[int.CreateSaturating(ind)] = TInt.Clamp(ind, TInt.Zero, count);
        indexBuffer[int.CreateSaturating(count - TInt.One)] = TInt.Zero;
        return;
    }
}
