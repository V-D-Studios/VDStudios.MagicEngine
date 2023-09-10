using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single circle
/// </summary>
public class CircleDefinition : ShapeDefinition2D
{
    private readonly Vector2[] VertexBuffer;

    /// <summary>
    /// The center point of the circle
    /// </summary>
    public Vector2 CenterPoint { get; }

    /// <summary>
    /// The radius of the circle
    /// </summary>
    public Radius Radius { get; }

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int Subdivisions { get; }

    /// <inheritdoc/>
    public override int Count => Subdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <summary>
    /// Instances a new object of type <see cref="CircleDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the circle</param>
    /// <param name="radius">The length of each point along the circle from its center, or half its diameter</param>
    /// <param name="subdivisions">The amount of vertices the circle will have. Must be larger than 3</param>
    public CircleDefinition(Vector2 centerPoint, Radius radius, int subdivisions = 30) : base(true)
    {
        if (subdivisions < 3)
            throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(subdivisions));

        if (radius.Value <= 0)
            throw new ArgumentException("A Circumference's Radius cannot be less or equal to 0", nameof(subdivisions));

        CenterPoint = centerPoint;
        Radius = radius;
        Subdivisions = subdivisions;

        VertexBuffer = new Vector2[Subdivisions];
        GenerateVertices(CenterPoint, Radius, Subdivisions, VertexBuffer.AsSpan());
    }

    /// <summary>
    /// Generates a list of vertices using the given information
    /// </summary>
    /// <remarks>
    /// This is the method used internally by <see cref="CircleDefinition"/>, can be used to generate vertices into an external buffer separately from a <see cref="CircleDefinition"/> instance. To do it relative to an instance, consider using <see cref="CopyTo(Span{Vector2})"/> instead
    /// </remarks>
    /// <param name="center">The centerpoint of the circle</param>
    /// <param name="radius">The radius of the circle</param>
    /// <param name="subdivisions">The amount of vertices to subdivide the circle into</param>
    /// <param name="buffer">The location in memory into which to store the newly generated vertices</param>
    /// <param name="angle">The portion of the circle to generate vertices for. For example: <c><see cref="float.Tau"/> / 2</c> would yield a half circle with <paramref name="subdivisions"/> subdivisions</param>
    public static void GenerateVertices(Vector2 center, Radius radius, int subdivisions, Span<Vector2> buffer, float angle = float.Tau)
    {
        var pbuf = new Vector2(center.X - radius, center.Y);
        var rot = Matrix3x2.CreateRotation(-angle / subdivisions, center);

        for (int i = 0; i < subdivisions; i++)
        {
            buffer[i] = pbuf;
            pbuf = Vector2.Transform(pbuf, rot);
        }
    }

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => VertexBuffer.AsSpan(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => VertexBuffer.AsSpan()[start..];

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan()
        => VertexBuffer;

    /// <inheritdoc/>
    public override void CopyTo(Span<Vector2> destination)
    {
        VertexBuffer.CopyTo(destination);
    }

    /// <inheritdoc/>
    public override IEnumerator<Vector2> GetEnumerator()
    {
        for (int i = 0; i < VertexBuffer.Length; i++)
            yield return VertexBuffer[i];
    }

    /// <inheritdoc/>
    public override Vector2[] ToArray()
    {
        var ret = new Vector2[VertexBuffer.Length];
        VertexBuffer.CopyTo(ret.AsSpan());
        return ret;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
    {
        return VertexBuffer.AsSpan().TryCopyTo(destination);
    }

    /// <inheritdoc/>
    public override int GetTriangulationLength(ElementSkip vertexSkip = default)
        => PolygonDefinition.GetPolygonTriangulationLength(Count, true, vertexSkip);

    /// <inheritdoc/>
    public override int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default)
        => PolygonDefinition.TriangulatePolygon(Count, true, outputIndices, vertexSkip);

    /// <inheritdoc/>
    public override int Triangulate(Span<ushort> outputIndices, ElementSkip vertexSkip = default)
        => PolygonDefinition.TriangulatePolygon(Count, IsConvex, outputIndices, vertexSkip);
}
