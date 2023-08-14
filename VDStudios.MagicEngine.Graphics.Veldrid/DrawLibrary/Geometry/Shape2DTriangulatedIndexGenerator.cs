using System.Numerics;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary.Geometry;

/// <summary>
/// An index generator that generates indices that, when uploaded to an index buffer, cause the rendering pipeline to draw triangles across the shape
/// </summary>
public class Shape2DTriangulatedIndexGenerator : IShape2DRendererIndexGenerator
{
    /// <summary>
    /// The <see cref="DonutDefinition"/> index generator to handle <see cref="DonutDefinition"/>s
    /// </summary>
    public DonutShape2DTriangulatedIndexGenerator DonutTriangulator { get; } = new();

    /// <inheritdoc/>
    public void Start(IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context)
    {
    }

    /// <inheritdoc/>
    public void GenerateUInt16(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, Span<ushort> indices, CommandList commandList, DeviceBuffer indexBuffer, int index, int indexCount, ElementSkip vertexSkip, uint indexStart, uint indexSize, out bool isBufferReady, ref object? context)
    {
        if (shape is DonutDefinition)
        {
            DonutTriangulator.GenerateUInt16(shape, allShapes, indices, commandList, indexBuffer, index, indexCount, vertexSkip, indexStart, indexStart, out isBufferReady, ref context);
            return;
        }
        var count = shape.Count;
        var step = vertexSkip.GetSkipFactor(shape.Count);
        ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(count), out var start);

        #region Triangle

        if (indexCount is 3)
        {
            commandList.UpdateBuffer(indexBuffer, indexStart, stackalloc ushort[4]
            {
                0,
                (ushort)step,
                (ushort)(count - 1),
                0,
            });
            isBufferReady = true;
            return;
        }

        #endregion

        #region Convex

        if (shape.IsConvex)
        {
            if (indices.Length is 0 && indexCount is 4)
            {
                commandList.UpdateBuffer(indexBuffer, indexStart, stackalloc ushort[6]
                {
                    (ushort)(1 * step), // 1
                    (ushort)(0 * step), // 0
                    (ushort)(3 * step), // 3
                    (ushort)(1 * step), // 1
                    (ushort)(2 * step), // 2
                    (ushort)int.Min(3 * step, count - 1)  // 3
                });
                isBufferReady = true;
                return;
            }

            GenerateConvexTriangulatedIndices((ushort)count, indices, (ushort)step, start);
            isBufferReady = false;
            return;
        }

        #endregion

        #region Concave

        GenerateConcaveTriangulatedIndices((ushort)count, indices, shape.AsSpan(), (ushort)step);
        isBufferReady = false;
        return;

        #endregion
    }

    /// <inheritdoc/>
    public bool QueryUInt16BufferSize(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, int index, ElementSkip vertexSkip, out int indexCount, out int indexSpace, ref object? context)
    {
        if (shape is DonutDefinition)
            return DonutTriangulator.QueryUInt16BufferSize(shape, allShapes, index, vertexSkip, out indexCount, out indexSpace, ref context);
        if (shape.IsConvex)
        {
            var count = shape.Count;
            if (count > 4)
            {
                indexCount = ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(count), out _);
                indexSpace = ComputeConvexTriangulatedIndexBufferSize(count, out _);
            }
            else
                indexCount = indexSpace = ComputeConvexTriangulatedIndexBufferSize(count, out _);

            if (indexCount is 3)
            {
                indexCount = 4;
                indexSpace = count is 3 ? 4 : indexSpace;
                return false;
            }

            if (indexCount is 4)
            {
                indexCount = 6;
                indexSpace = count is 4 ? 6 : indexSpace;
                return false;
            }

            return true;
        }

        throw new NotSupportedException("Non convex shapes are not supported!");
    }

    /// <inheritdoc/>
    public void Stop(ref object? context)
    {
    }

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store triangulated indices for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <param name="added">A number, either a <c>1</c> or <c>0</c>, that offsets the triangulation to account for odd vertex counts</param>
    public static int ComputeConvexTriangulatedIndexBufferSize(int vertexCount, out byte added)
        => (vertexCount - (added = vertexCount % 2 == 0 ? (byte)0u : (byte)1u)) * 3;

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a triangle strip
    /// </summary>
    /// <typeparam name="TInt">The type of integer to fill the buffer with</typeparam>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    /// <param name="start">The starting point of the triangulation. Synonym to <c>added</c> in <see cref="ComputeConvexTriangulatedIndexBufferSize(int, out byte)"/></param>
    public static void GenerateConvexTriangulatedIndices<TInt>(TInt count, Span<TInt> indexBuffer, TInt step, byte start = 0) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        TInt i = TInt.CreateSaturating(start);

        while (i < count)
        {
            indexBuffer[bufind++] = step * TInt.Zero;
            indexBuffer[bufind++] = step * i++;
            indexBuffer[bufind++] = step * i;
        }
    }

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store triangulated indices for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <param name="added">A number, either a <c>1</c> or <c>0</c>, that offsets the triangulation to account for odd vertex counts</param>
    /// <param name="vertices">The buffer that contains the vertices that are to be indexed</param>
    public static int ComputeConcaveTriangulatedIndexBufferSize(int vertexCount, out byte added, ReadOnlySpan<Vector2> vertices)
        => (vertexCount - (added = vertexCount % 2 == 0 ? (byte)0u : (byte)1u)) * 3;

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a triangle strip
    /// </summary>
    /// <typeparam name="TInt">The type of integer to fill the buffer with</typeparam>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    /// <param name="vertices">The buffer that contains the vertices that are to be indexed</param>
    /// <param name="start">The starting point of the triangulation. Synonym to <c>added</c> in <see cref="ComputeConcaveTriangulatedIndexBufferSize(int, out byte, ReadOnlySpan{Vector2})"/></param>
    [Obsolete("This method is not yet implemented")]
    public static void GenerateConcaveTriangulatedIndices<TInt>(TInt count, Span<TInt> indexBuffer, ReadOnlySpan<Vector2> vertices, TInt step, byte start = 0) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        int c = int.CreateChecked(count);
        int s = int.CreateChecked(step);

        //while true
        //  for every vertex
        //    let pPrev = the previous vertex in the list
        //    let pCur = the current vertex;
        //    let pNext = the next vertex in the list
        //    if the vertex is not an interior vertex (the wedge product of (pPrev - pCur) and (pNext - pCur) <= 0, for CCW winding);
        //      continue;
        //    if there are any vertices in the polygon inside the triangle made by the current vertex and the two adjacent ones
        //      continue;
        //    create the triangle with the points pPrev, pCur, pNext, for a CCW triangle;
        //    remove pCur from the list;
        //  if no triangles were made in the above for loop
        //    break;

        Vector2 prev;
        Vector2 curr;
        Vector2 next;
        while (true)
        {
            for (int i = c; i >= 0; i -= s)
            {
                prev = i < c ? vertices[i + 1] : vertices[i];
                curr = vertices[i];
                next = i > 0 ? vertices[i - 1] : vertices[i];
                if ((prev - curr).Cross(next - curr) <= 0)
                    continue;
            }
        }
    }
}