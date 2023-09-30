using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Serilog;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single polygon, starting from the furthest to the bottom and to the left, in CW order
/// </summary>
/// <remarks>
/// Vertices should be defined in a space relative to themselves, as transformations and positions should be handled by the owner of the definition
/// </remarks>
public class PolygonDefinition : ShapeDefinition2D, IStructuralEquatable
{
    private readonly Vector2[] Vertices;

    /// <summary>
    /// The current vertex count, or -1, if <c>default</c>
    /// </summary>
    public override int Count => Vertices?.Length ?? -1;

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/> up until <paramref name="vertexCount"/>, or until the length of <paramref name="vertices"/> if it's a negative number
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    /// <param name="vertexCount">The amount of vertices to take from <paramref name="vertices"/>. <c>0</c> represents all indices, negative numbers start from the end</param>
    /// <param name="isConvex">Whether or not this shape is a convex shape. If <see langword="false"/> then the shape will considered concave</param>
    public PolygonDefinition(Vector2[] vertices, bool isConvex, int vertexCount = 0)
        : this(vertices.AsSpan(0, vertexCount is 0 ? vertices.Length : vertexCount > 0 ? Index.FromStart(vertexCount).GetOffset(vertices.Length) : Index.FromEnd(vertexCount).GetOffset(vertices.Length)), isConvex) { }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/>
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    /// <param name="isConvex">Whether or not this shape is a convex shape. If <see langword="false"/> then the shape will considered concave</param>
    public PolygonDefinition(ReadOnlySpan<Vector2> vertices, bool isConvex) : base(isConvex)
    {
        if (vertices.Length is < 3)
            throw new ArgumentException("A polygon must have at least 3 vertices", nameof(vertices));
        Vertices = new Vector2[vertices.Length];
        vertices.CopyTo(Vertices);
    }

    /// <inheritdoc/>
    public bool Equals(object? other, IEqualityComparer comparer)
    {
        return ((IStructuralEquatable)Vertices).Equals(other, comparer);
    }

    /// <inheritdoc/>
    public int GetHashCode(IEqualityComparer comparer)
    {
        return ((IStructuralEquatable)Vertices).GetHashCode(comparer);
    }

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => Vertices.AsSpan(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => Vertices.AsSpan(start);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan()
        => Vertices.AsSpan();

    /// <inheritdoc/>
    public override void CopyTo(Span<Vector2> destination)
    {
        ((Span<Vector2>)Vertices).CopyTo(destination);
    }

    /// <inheritdoc/>
    public override IEnumerator<Vector2> GetEnumerator() => ((IEnumerable<Vector2>)Vertices).GetEnumerator();

    /// <inheritdoc/>
    public override Vector2[] ToArray()
    {
        var ret = new Vector2[Vertices.Length];
        Vertices.CopyTo(ret, 0);
        return ret;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
    {
        return ((Span<Vector2>)Vertices).TryCopyTo(destination);
    }

    /// <inheritdoc/>
    public override Vector2 this[int index] => Vertices[index];

    /// <inheritdoc/>
    public override int GetTriangulationLength(ElementSkip vertexSkip = default)
        => GetPolygonTriangulationLength(Count, IsConvex, vertexSkip);

    /// <summary>
    /// Calculates how long a span of <see cref="uint"/> indices need to be for a triangulation operation on this object
    /// </summary>
    public static int GetPolygonTriangulationLength(int vertexCount, bool isConvex, ElementSkip vertexSkip = default)
    {
        if (isConvex)
        {
            int indexCount = vertexCount > 4
                ? ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(vertexCount), out _)
                : ComputeConvexTriangulatedIndexBufferSize(vertexCount, out _);

            return indexCount is 3 ? 4 : indexCount is 4 ? 9 : indexCount;
        }

        throw new NotSupportedException("Non convex shapes are not supported!");
    }

    /// <inheritdoc/>
    public override int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default)
        => TriangulatePolygon(Count, IsConvex, outputIndices, vertexSkip);

    /// <inheritdoc/>
    public override int Triangulate(Span<ushort> outputIndices, ElementSkip vertexSkip = default)
        => TriangulatePolygon(Count, IsConvex, outputIndices, vertexSkip);

    /// <summary>
    /// Triangulates a shape into a set of indices that run through its vertices as a set of triangles
    /// </summary>
    /// <param name="outputIndices">The output of the operation. See <see cref="GetTriangulationLength"/></param>
    /// <param name="vertexSkip">The amount of vertices to skip when triangulating</param>
    /// <param name="isConvex">Whether or not the Polygon is convex</param>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <returns>The amount of indices written to <paramref name="outputIndices"/></returns>
    public static int TriangulatePolygon(int vertexCount, bool isConvex, Span<uint> outputIndices, ElementSkip vertexSkip = default)
    {
        var count = (uint)vertexCount;
        var step = (uint)vertexSkip.GetSkipFactor(vertexCount);
        ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(vertexCount), out var start);
        int indexCount = GetPolygonTriangulationLength(vertexCount, true, vertexSkip);

        #region Triangle

        if (indexCount is 3)
        {
            outputIndices[0] = 0;
            outputIndices[1] = step;
            outputIndices[2] = (count - 1);
            outputIndices[3] = 0;
            return 4;
        }

        #endregion

        #region Convex

        if (isConvex)
        {
            if (indexCount is 9)
            {
                for (int i = 0; i < RectangleDefinition.TriangulatedRectangleUInt32.Length; i++)
                    outputIndices[i] = RectangleDefinition.TriangulatedRectangleUInt32[i] * step;
                return 9;
            }

            ComputeConvexTriangulatedIndexBuffers(count, outputIndices, step, start);
            return outputIndices.Length;
        }

        #endregion

        #region Concave

        throw new NotSupportedException("Non convex shapes are not supported!");
        //GenerateConcaveTriangulatedIndices((ushort)count, indices, shape.AsSpan(), (ushort)step);
        //isBufferReady = false;
        //return;

        #endregion
    }

    /// <summary>
    /// Triangulates a shape into a set of indices that run through its vertices as a set of triangles
    /// </summary>
    /// <param name="outputIndices">The output of the operation. See <see cref="GetTriangulationLength"/></param>
    /// <param name="vertexSkip">The amount of vertices to skip when triangulating</param>
    /// <param name="isConvex">Whether or not the Polygon is convex</param>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <returns>The amount of indices written to <paramref name="outputIndices"/></returns>
    public static int TriangulatePolygon(int vertexCount, bool isConvex, Span<ushort> outputIndices, ElementSkip vertexSkip = default)
    {
        var count = vertexCount;
        var step = (ushort)vertexSkip.GetSkipFactor(vertexCount);
        ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(count), out var start);
        int indexCount = GetPolygonTriangulationLength(vertexCount, true, vertexSkip);

        #region Triangle

        if (indexCount is 3)
        {
            outputIndices[0] = 0;
            outputIndices[1] = step;
            outputIndices[2] = (ushort)(count - 1);
            outputIndices[3] = 0;
            return 4;
        }

        #endregion

        #region Convex

        if (isConvex)
        {
            if (indexCount is 9)
            {
                for (int i = 0; i < RectangleDefinition.TriangulatedRectangleUInt16.Length; i++)
                    outputIndices[i] = (ushort)(RectangleDefinition.TriangulatedRectangleUInt16[i] * step);
                return 9;
            }

            ComputeConvexTriangulatedIndexBuffers((ushort)count, outputIndices, step, start);
            return outputIndices.Length;
        }

        #endregion

        #region Concave

        throw new NotSupportedException("Non convex shapes are not supported!");
        //GenerateConcaveTriangulatedIndices((ushort)count, indices, shape.AsSpan(), (ushort)step);
        //isBufferReady = false;
        //return;

        #endregion
    }

    /// <summary>
    /// Computes the appropriate size for an index buffer that will store triangulated indices for a 2D shape of <paramref name="vertexCount"/> vertices
    /// </summary>
    /// <param name="vertexCount">The amount of vertices the shape has</param>
    /// <param name="added">A number, either a <c>1</c> or <c>0</c>, that offsets the triangulation to account for odd vertex counts</param>
    public static int ComputeConvexTriangulatedIndexBufferSize(int vertexCount, out byte added)
        => (vertexCount - (added = vertexCount % 2 == 0 ? (byte)0u : (byte)1u)) * 3;

#if DEBUG
    /// <inheritdoc/>
    public override void RegenVertices()
    {
        Log.Error("PolygonDefinition: Cannot regenerate the vertices of an arbitrary polygon");
    }
#endif

    /// <summary>
    /// Generates indices for a shape that is to be rendered as a triangle strip
    /// </summary>
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    /// <param name="step">The step factor for skipping</param>
    /// <param name="start">The starting point of the triangulation. Synonym to <c>added</c> in <see cref="ComputeConvexTriangulatedIndexBufferSize(int, out byte)"/></param>
    public static void ComputeConvexTriangulatedIndexBuffers<TInt>(TInt count, Span<TInt> indexBuffer, TInt step, byte start)
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        TInt i = TInt.CreateSaturating(start);

        while (i < count)
        {
            indexBuffer[bufind++] = TInt.Zero;
            indexBuffer[bufind++] = step * i++;
            indexBuffer[bufind++] = step * i;
        }
    }
}