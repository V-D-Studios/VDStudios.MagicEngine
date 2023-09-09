using System.Numerics;
using VDStudios.MagicEngine;

namespace VDStudios.MagicEngine.Geometry;
public class ElipseDefinition : ShapeDefinition2D
{
    private readonly Vector2[] ___vertexBuffer;
    private Span<Vector2> VertexBuffer
        => ___vertexBuffer;

    /// <summary>
    /// The center point of the elipse
    /// </summary>
    public Vector2 CenterPoint { get; }

    /// <summary>
    /// The radius of the elipse along the x axis
    /// </summary>
    public Radius RadiusX { get; }

    /// <summary>
    /// The radius of the elipse along the y axis
    /// </summary>
    public Radius RadiusY { get; }

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int Subdivisions { get; }

    /// <inheritdoc/>
    public override int Count => Subdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <summary>
    /// Instances a new object of type <see cref="ElipseDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the elipse</param>
    /// <param name="radiusX">The length of each point along the elipse from its center along its x axis</param>
    /// <param name="radiusY">The length of each point along the elipse from its center along its y axis</param>
    /// <param name="subdivisions">The amount of vertices the elipse will have. Must be larger than 3</param>
    public ElipseDefinition(Vector2 centerPoint, Radius radiusX, Radius radiusY, int subdivisions = 30) : base(true)
    {
        CenterPoint = centerPoint;
        RadiusX = radiusX;
        RadiusY = radiusY;
        Subdivisions = subdivisions;

        if (subdivisions < 3)
            throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(subdivisions));

        ___vertexBuffer = new Vector2[Subdivisions];
        GenerateVertices(CenterPoint, RadiusX, RadiusY, Subdivisions, ___vertexBuffer.AsSpan(0, Subdivisions));
    }

    /// <summary>
    /// Generates a list of vertices using the given information
    /// </summary>
    /// <remarks>
    /// This is the method used internally by <see cref="ElipseDefinition"/>, can be used to generate vertices into an external buffer separately from a <see cref="ElipseDefinition"/> instance. To do it relative to an instance, consider using <see cref="CopyTo(Span{Vector2})"/> instead
    /// </remarks>
    /// <param name="center">The centerpoint of the elipse</param>
    /// <param name="radiusX">The radius of the elipse along the x axis</param>
    /// <param name="radiusY">The radius of the elipse along the y axis</param>
    /// <param name="subdivisions">The amount of vertices to subdivide the elipse into</param>
    /// <param name="buffer">The location in memory into which to store the newly generated vertices</param>
    public static void GenerateVertices(Vector2 center, Radius radiusX, Radius radiusY, int subdivisions, Span<Vector2> buffer)
    {
        //(radiusY, radiusX) = (radiusX, radiusY);
        var pbuf = new Vector2(center.X - radiusX, 0);
        var trans = Matrix3x2.CreateRotation(-float.Tau / subdivisions, center);
        var scale = radiusX.Diameter.CompareTo(radiusY.Diameter) switch
        {
            < 0 => Matrix3x2.CreateScale(1f, radiusY / radiusX, center),
            > 0 => Matrix3x2.CreateScale(1f, radiusY / radiusX, center),
            _ => Matrix3x2.Identity
        };

        for (int i = 0; i < subdivisions; i++)
        {
            buffer[i] = Vector2.Transform(pbuf, scale);
            pbuf = Vector2.Transform(pbuf, trans);
        }
    }

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => VertexBuffer.Slice(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => VertexBuffer.Slice(start);

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
        VertexBuffer.CopyTo(ret);
        return ret;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
    {
        return VertexBuffer.TryCopyTo(destination);
    }

    /// <inheritdoc/>
    public override int GetTriangulationLength(ElementSkip vertexSkip = default)
        => PolygonDefinition.GetPolygonTriangulationLength(Count, true, vertexSkip);

    /// <inheritdoc/>
    public override int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default)
        => PolygonDefinition.TriangulatePolygon(Count, true, outputIndices, vertexSkip);
}
