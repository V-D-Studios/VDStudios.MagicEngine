﻿using System.Numerics;
using VDStudios.MagicEngine;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single elipse
/// </summary>
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

    /// <summary>
    /// The portion of the elipse this definition represents
    /// </summary>
    public float Angle { get; }
    
    /// <summary>
    /// Whether this is a full elipse
    /// </summary>
    public bool IsFull => float.Abs(Angle) == float.Tau;

    /// <summary>
    /// Whether or not this elipse's vertices are ordered clockwise
    /// </summary>
    public bool IsClockwise => Angle < 0;

    /// <inheritdoc/>
    public override int Count => IsFull ? Subdivisions : Subdivisions + 1;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <summary>
    /// Instances a new object of type <see cref="ElipseDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the elipse</param>
    /// <param name="radiusX">The length of each point along the elipse from its center along its x axis</param>
    /// <param name="radiusY">The length of each point along the elipse from its center along its y axis</param>
    /// <param name="subdivisions">The amount of vertices the elipse will have. Must be larger than 3</param>
    /// <param name="angle">The portion of the ellipse to generate vertices for. For example: <c>-<see cref="float.Tau"/> / 2</c> would yield a half circle with <paramref name="subdivisions"/> subdivisions</param>
    public ElipseDefinition(Vector2 centerPoint, Radius radiusX, Radius radiusY, int subdivisions = 30, float angle = -float.Tau) : base(true)
    {
        Angle = angle;
        CenterPoint = centerPoint;
        RadiusX = radiusX;
        RadiusY = radiusY;
        Subdivisions = subdivisions;

        if (subdivisions < 3)
            throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(subdivisions));

        ___vertexBuffer = new Vector2[Count];
        GenerateVertices(CenterPoint, RadiusX, RadiusY, Subdivisions, ___vertexBuffer.AsSpan(0, Count), angle);
    }

    /// <summary>
    /// Gets the starting vertex of the ellipse
    /// </summary>
    /// <remarks>
    /// Used internally by <see cref="GenerateVertices(Vector2, Radius, Radius, int, Span{Vector2}, float)"/> and <see cref="CircleDefinition.GenerateVertices(Vector2, Radius, int, Span{Vector2}, float)"/>
    /// </remarks>
    /// <param name="center">The centerpoint of the ellipse</param>
    /// <param name="radiusX">The radius of the ellipse along the X axis</param>
    /// <param name="subdivisions">The amount of vertices to subdivide the elipse into</param>
    /// <param name="radiusY">The radius of the ellipse along the Y axis</param>
    /// <param name="angle">The portion of the ellipse to generate vertices for. For example: <c>-<see cref="float.Tau"/> / 2</c> would yield a half circle with <paramref name="subdivisions"/> subdivisions</param>
    public static Vector2 GetStartingPoint(Vector2 center, Radius radiusX, Radius radiusY, int subdivisions, float angle = -float.Tau)
    {
        var trans = Matrix3x2.CreateRotation(angle / subdivisions / 2, center);
        return Vector2.Transform(radiusY > radiusX ? new Vector2(center.X, center.Y - radiusX) : new Vector2(center.X - radiusY, center.Y), trans);
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
    /// <param name="angle">The portion of the ellipse to generate vertices for. For example: <c>-<see cref="float.Tau"/> / 2</c> would yield a half circle with <paramref name="subdivisions"/> subdivisions</param>
    public static void GenerateVertices(Vector2 center, Radius radiusX, Radius radiusY, int subdivisions, Span<Vector2> buffer, float angle = -float.Tau)
    {
        int i = 0;
        if (float.Abs(angle) != float.Tau)
        {
            subdivisions += 1;
            buffer[i++] = center;
        }

        var pbuf = GetStartingPoint(center, radiusX, radiusY, subdivisions, angle);
        var trans = Matrix3x2.CreateRotation(angle / subdivisions, center);
        var scale = radiusX.Diameter.CompareTo(radiusY.Diameter) switch
        {
            < 0 => Matrix3x2.CreateScale(1f, radiusY / radiusX, center),
            > 0 => Matrix3x2.CreateScale(radiusX / radiusY, 1f, center),
            _ => Matrix3x2.Identity
        };

        for (; i < subdivisions; i++)
        {
            buffer[i] = Vector2.Transform(pbuf, scale);
            pbuf = Vector2.Transform(pbuf, trans);
        }
    }

#if DEBUG
    /// <inheritdoc/>
    public override void RegenVertices()
    {
        GenerateVertices(CenterPoint, RadiusX, RadiusY, Subdivisions, ___vertexBuffer.AsSpan(0, Count), Angle);
    }
#endif

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

    /// <inheritdoc/>
    public override int Triangulate(Span<ushort> outputIndices, ElementSkip vertexSkip = default)
        => PolygonDefinition.TriangulatePolygon(Count, IsConvex, outputIndices, vertexSkip);
}
