using System.Buffers;
using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition for a segment that has a point A, a point B and a width
/// </summary>
public class SegmentDefinition : ShapeDefinition2D
{
    private readonly Vector2[] ___vertices = new Vector2[4];

    private Span<Vector2> Vertices
        => ___vertices;

    /// <summary>
    /// Instances a new object of type <see cref="SegmentDefinition"/>
    /// </summary>
    /// <param name="a">The starting point of the segment</param>
    /// <param name="b">The ending point of the segment</param>
    /// <param name="width">The width of the segment</param>
    public SegmentDefinition(Vector2 a, Vector2 b, float width = 1) : base(true)
    {
        PointA = a;
        PointB = b;
        Width = width;

        GenerateVertices(a, b, ___vertices, width);
    }

    /// <summary>
    /// Generates the 4 vertices describing a segment that has a width into <paramref name="output"/>
    /// </summary>
    /// <param name="a">The starting point of the segment</param>
    /// <param name="b">The ending point of the segment</param>
    /// <param name="width">The width of the segment</param>
    /// <param name="output">The buffer that will contain the output vertices, must have a length of 4 or more</param>
    /// <exception cref="ArgumentException">Thrown if output doesn't have a length of at least 4</exception>
    public static void GenerateVertices(Vector2 a, Vector2 b, Span<Vector2> output, float width = 1)
    {
        Matrix3x2 rotation = Matrix3x2.CreateRotation(GeometryMath.Angle(a, b), a);
        var clampedB = Vector2.Transform(b, -rotation);

        RectangleDefinition.GenerateVertices(a, new(Vector2.Distance(a, clampedB), width), output);

        for (int i = 0; i < 4; i++)
            output[i] = Vector2.Transform(output[i], rotation);
    }

    /// <summary>
    /// The starting point of the segment
    /// </summary>
    public Vector2 PointA { get; }

    /// <summary>
    /// The ending point of the segment
    /// </summary>
    public Vector2 PointB { get; }

    /// <summary>
    /// The width of the segment
    /// </summary>
    public float Width { get; }

#warning Add variable width along the points
#warning Add the option to add an amount of vertices to the end of the lines for smoothing, where 0 would be a flat line, and one would make the segment end with a triangle

#if DEBUG
    /// <inheritdoc/>
    public override void RegenVertices()
    {
        GenerateVertices(PointA, PointB, Vertices, Width);
    }
#endif

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => Vertices.Slice(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => Vertices.Slice(start);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan()
        => Vertices;

    /// <inheritdoc/>
    public override void CopyTo(Span<Vector2> destination)
        => Vertices.CopyTo(destination);

    /// <inheritdoc/>
    public override IEnumerator<Vector2> GetEnumerator()
        => ((IEnumerable<Vector2>)___vertices).GetEnumerator();

    /// <inheritdoc/>
    public override Vector2[] ToArray()
    {
        var x = new Vector2[4];
        Vertices.CopyTo(x);
        return x;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
    {
        if (destination.Length < 4)
            return false;
        Vertices.CopyTo(destination);
        return true;
    }

    /// <inheritdoc/>
    public override Vector2 this[int index] => Vertices[index];

    /// <inheritdoc/>
    public override int Count => 4;

    /// <inheritdoc/>
    public override int GetTriangulationLength(ElementSkip vertexSkip = default)
        => vertexSkip.GetSkipFactor(4) != 0
            ? throw new ArgumentException("RectangleDefinition does not support skipping vertices", nameof(vertexSkip))
            : RectangleDefinition.TriangulatedRectangleUInt32.Length;

    /// <inheritdoc/>
    public override int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default)
    {
        if (vertexSkip.GetSkipFactor(4) != 0)
            throw new ArgumentException("RectangleDefinition does not support skipping vertices", nameof(vertexSkip));
        RectangleDefinition.TriangulatedRectangleUInt32.CopyTo(outputIndices);
        return RectangleDefinition.TriangulatedRectangleUInt32.Length;
    }

    /// <inheritdoc/>
    public override int Triangulate(Span<ushort> outputIndices, ElementSkip vertexSkip = default)
    {
        if (vertexSkip.GetSkipFactor(4) != 0)
            throw new ArgumentException("RectangleDefinition does not support skipping vertices", nameof(vertexSkip));
        RectangleDefinition.TriangulatedRectangleUInt16.CopyTo(outputIndices);
        return RectangleDefinition.TriangulatedRectangleUInt16.Length;
    }
}
