using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single polygon, starting from the furthest to the bottom and to the left, in CW order
/// </summary>
/// <remarks>
/// Vertices should be defined in a space relative to themselves, as transformations and positions should be handled by the owner of the definition
/// </remarks>
public class PolygonDefinition : ShapeDefinition2D, IStructuralEquatable
{
    #region Predefined Polygons

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Circle
    /// </summary>
    /// <remarks>
    /// This method creates a Polygon whose vertices are all equally distant from <paramref name="center"/> by <paramref name="radius"/>. To define an actual circle, see <see cref="CircleDefinition"/>
    /// </remarks>
    /// <param name="center">The center point of the circle</param>
    /// <param name="radius">The length of each point along the circle from its center, or half its diameter</param>
    /// <param name="subdivisions">The amount of vertices the circle will have. Must be larger than 3</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with <paramref name="subdivisions"/> vertices describing the circle</returns>
    public static PolygonDefinition Circle(Vector2 center, float radius, int subdivisions = 30)
    {
        if (subdivisions < 3)
            throw new ArgumentException("Subdivisions cannot be less than 3", nameof(subdivisions));

        var pbuf = center with { X = center.X + radius };
        var rot = Matrix3x2.CreateRotation(MathF.Tau / subdivisions, center);

        Span<Vector2> vertices = subdivisions > 5000 ? new Vector2[subdivisions] : stackalloc Vector2[subdivisions];
        for (int i = 0; i < subdivisions; i++)
        {
            vertices[i] = pbuf;
            pbuf = Vector2.Transform(pbuf, rot);
        }

        return new PolygonDefinition(vertices, true);
    }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle represented by <paramref name="rectangle"/>
    /// </summary>
    /// <param name="rectangle">The rectangle describing the location and dimensions of the polygon to define</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(FloatRectangle rectangle)
    {
        var (w, h, x, y) = rectangle;
        return Rectangle(new(x, y), new(w, h));
    }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle located at <paramref name="x"/> and <paramref name="y"/> with dimensions of <paramref name="width"/> and <paramref name="height"/>
    /// </summary>
    /// <param name="x">The location of the Rectangle along the <c>X</c> axis</param>
    /// <param name="y">The location of the Rectangle along the <c>Y</c> axis</param>
    /// <param name="width">The width of the Rectangle</param>
    /// <param name="height">The height of the Rectangle</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(float x, float y, float width, float height)
        => Rectangle(new(x, y), new(width, height));

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle located at <paramref name="x"/> and <paramref name="y"/> with dimensions <paramref name="size"/>
    /// </summary>
    /// <param name="x">The location of the Rectangle along the <c>X</c> axis</param>
    /// <param name="y">The location of the Rectangle along the <c>Y</c> axis</param>
    /// <param name="size">The size of the Rectangle, with <see cref="Vector2.X"/> being the width, and <see cref="Vector2.Y"/> being the height</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(float x, float y, Vector2 size)
        => Rectangle(new(x, y), size);

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle located at <paramref name="position"/> with dimensions of <paramref name="width"/> and <paramref name="height"/>
    /// </summary>
    /// <param name="position">The position of the Rectangle</param>
    /// <param name="width">The width of the Rectangle</param>
    /// <param name="height">The height of the Rectangle</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(Vector2 position, float width, float height)
        => Rectangle(position, new(width, height));

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle located at <paramref name="position"/> with dimensions <paramref name="size"/>
    /// </summary>
    /// <param name="position">The position of the Rectangle</param>
    /// <param name="size">The size of the Rectangle, with <see cref="Vector2.X"/> being the width, and <see cref="Vector2.Y"/> being the height</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(Vector2 position, Vector2 size) => new(stackalloc Vector2[]
    {
        position,
        new(position.X, position.Y + size.Y),
        position + size,
        new(position.X + size.X, position.Y)
    }, true);

    /// <summary>
    /// Fills <paramref name="output"/> with the vertices of a rectangle created with the given parameters
    /// </summary>
    /// <param name="position">The position of the Rectangle</param>
    /// <param name="size">The size of the Rectangle, with <see cref="Vector2.X"/> being the width, and <see cref="Vector2.Y"/> being the height</param>
    /// <param name="output">The span that will hold the data from the created rectangle</param>
    public static void Rectangle(Vector2 position, Vector2 size, Span<Vector2> output)
    {
        if (output.Length < 4)
            throw new ArgumentException("output must have a length of at least 4", nameof(output));

        output[0] = position;
        output[1] = new(position.X, position.Y + size.Y);
        output[2] = position + size;
        output[3] = new(position.X + size.X, position.Y);
    }

    #endregion

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
            var count = vertexCount;

            return count > 4
                ? ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(count), out _)
                : vertexSkip.GetSkipFactor(4) != 0
                ? throw new ArgumentException("Polygons with less than 5 vertices do not support skipping vertices", nameof(vertexSkip))
                : count is 3
                ? 4
                : count is 4 ? 6 : throw new InvalidOperationException("Cannot triangulate a polygon with less than 3 vertices");
        }
        else
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
    public static int TriangulatePolygon<TInt>(int vertexCount, bool isConvex, Span<TInt> outputIndices, ElementSkip vertexSkip = default)
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        var count = vertexCount;
        var step = vertexSkip.GetSkipFactor(vertexCount);
        int indexCount = ComputeConvexTriangulatedIndexBufferSize(vertexSkip.GetElementCount(count), out var start);

        if (indexCount is 3)
        {
            if (outputIndices.Length <= 4)
                throw new ArgumentException("The span is too short to hold the triangulated indices", nameof(outputIndices));

            outputIndices[0] = TInt.Zero;
            outputIndices[1] = TInt.CreateSaturating(step);
            outputIndices[2] = TInt.CreateSaturating(count - 1);
            outputIndices[3] = TInt.Zero;

            return 4;
        }

        if (isConvex)
        {
            if (indexCount is 4)
            {
                if (outputIndices.Length <= 6)
                    throw new ArgumentException("The span is too short to hold the triangulated indices", nameof(outputIndices));

                outputIndices[0] = TInt.CreateSaturating(1 * step); // 1
                outputIndices[1] = TInt.CreateSaturating(0 * step); // 0
                outputIndices[2] = TInt.CreateSaturating(3 * step); // 3
                outputIndices[3] = TInt.CreateSaturating(1 * step); // 1
                outputIndices[4] = TInt.CreateSaturating(2 * step); // 2
                outputIndices[5] = TInt.Min(TInt.CreateSaturating(3 * step), TInt.CreateSaturating(count - 1)); // 3

                return 6;
            }

            ComputeConvexTriangulatedIndexBuffers(count, outputIndices, TInt.CreateSaturating(step), start);
            return indexCount;
        }

        throw new NotSupportedException("Non convex shapes are not supported!");
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
    /// <param name="count">The amount of vertices in the shape</param>
    /// <param name="indexBuffer">The buffer to store the indices in</param>
    /// <param name="step">The step factor for skipping</param>
    /// <param name="start">The starting point of the triangulation. Synonym to <c>added</c> in <see cref="ComputeConvexTriangulatedIndexBufferSize(int, out byte)"/></param>
    public static void ComputeConvexTriangulatedIndexBuffers<TInt>(int count, Span<TInt> indexBuffer, TInt step, byte start)
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        TInt i = TInt.CreateSaturating(start);
        TInt c = TInt.CreateSaturating(count);

        while (i < c)
        {
            indexBuffer[bufind++] = TInt.Zero;
            indexBuffer[bufind++] = step * i++;
            indexBuffer[bufind++] = step * i;
        }
    }
}