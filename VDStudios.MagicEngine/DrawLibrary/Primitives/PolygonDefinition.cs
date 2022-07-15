using System;
using System.Collections;
using System.Numerics;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents the definition of a single polygon, starting from the furthest to the bottom and to the left, in CW order
/// </summary>
/// <remarks>
/// Vertices should be defined in a space relative to themselves, as transformations and positions should be handled by the owner of the definition
/// </remarks>
public struct PolygonDefinition : IReadOnlyList<Vector2>, IStructuralEquatable
{
    private readonly Vector2[] Vertices;

    #region Analysis

    private bool isConvexAnalyzed = false;
    private bool isConvex = false;

    /// <summary>
    /// <c>true</c> if this <see cref="PolygonDefinition"/> represents a Convex polygon. That is, a polygon with no internal angles greater than 180 deg
    /// </summary>
    public bool IsConvex
    {
        get
        {
            if (!isConvexAnalyzed)
            {
                isConvex = AnalyzeConvexity();
                isConvexAnalyzed = true;
            }
            return isConvex;
        }
    }

    /// <summary>
    /// <c>true</c> if this <see cref="PolygonDefinition"/> has self intersecting segments
    /// </summary>
    [Obsolete("This property is not fully implemented, and will not actually analyze the polygon.")]
    public bool IsSelfIntersecting
    {
        get
        {
            if (isConvexAnalyzed && isConvex)
                return false;
            return false;
        }
    }

    private bool AnalyzeConvexity() 
    {
        const float Pi = MathF.PI;
        const float TwoPi = MathF.PI * 2;

        if (Vertices.Length <= 3)
            return false;

        Vector2 prev = Vertices[^1];
        Vector2 curr = Vertices[^2];
        float odir;
        float ndir = MathF.Atan2(curr.Y - prev.Y, curr.X - prev.X);
        float anglesum = 0f;
        float angle;
        float orientation = 0f;

        for (int i = 0; i < Vertices.Length; i++)
        {
            prev = curr;
            odir = ndir;
            curr = Vertices[i];
            ndir = MathF.Atan2(curr.Y - prev.Y, curr.X - prev.X);
            if (prev == curr)
                return false;
            angle = ndir - odir;
            if (angle <= -Pi)
                angle += TwoPi;
            else if (angle > Pi)
                angle -= TwoPi;

            if (i is 0)
            {
                if (angle == 0)
                    return false;
                orientation = angle > 0 ? 1 : -1;
            }
            else if (orientation * angle <= 0)
                return false;

            anglesum += angle;
        }

        return MathF.Abs(MathF.Abs(MathF.Round(anglesum / TwoPi)) - 1) > 0.00001;
    }

    #endregion

    /// <summary>
    /// Queries this <see cref="PolygonDefinition"/> for the <see cref="Vector2"/> that represents the vertex at <paramref name="index"/>
    /// </summary>
    /// <param name="index">The index of the vertex to query</param>
    /// <returns>The <see cref="Vector2"/> at <paramref name="index"/></returns>
    public Vector2 this[int index] => Vertices is not null ? Vertices[index] : throw new InvalidOperationException("The default value for PolygonDefinition cannot be queried");

    /// <summary>
    /// The current vertex count, or -1, if <c>default</c>
    /// </summary>
    public int Count => Vertices?.Length ?? -1;

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/> up until <paramref name="vertexCount"/>, or until the length of <paramref name="vertices"/> if it's a negative number
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    /// <param name="vertexCount">The amount of vertices to take from <paramref name="vertices"/>, starting at index <c>0</c></param>
    public PolygonDefinition(Vector2[] vertices, int vertexCount = -1)
        : this(((Span<Vector2>)vertices).Slice(0, vertexCount > 0 ? vertexCount : vertices.Length)) { }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/>
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    public PolygonDefinition(ReadOnlySpan<Vector2> vertices)
    {
        if (vertices.Length is < 3)
            throw new ArgumentException("A polygon must have at least 3 vertices", nameof(vertices));
        Vertices = new Vector2[vertices.Length];
        vertices.CopyTo(Vertices);
    }

    /// <summary>
    /// Copies the vertices of this <see cref="PolygonDefinition"/> into a new array
    /// </summary>
    /// <returns>The newly created and now filled array</returns>
    public Vector2[] ToArray()
    {
        var ret = new Vector2[Vertices.Length];
        Vertices.CopyTo(ret, 0);
        return ret;
    }

    /// <summary>
    /// Creates a new Span over the portion of this <see cref="PolygonDefinition"/> beginning at <paramref name="start"/> for <paramref name="length"/>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => Vertices.AsSpan(start, length);

    /// <summary>
    /// Creates a new Span over the portion of this <see cref="PolygonDefinition"/> beginning at <paramref name="start"/> for the rest of this <see cref="PolygonDefinition"/>
    /// </summary>
    /// <param name="start"></param>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public ReadOnlySpan<Vector2> AsSpan(int start)
        => Vertices.AsSpan(start);

    /// <summary>
    /// Creates a new Span over this <see cref="PolygonDefinition"/>
    /// </summary>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public ReadOnlySpan<Vector2> AsSpan()
        => Vertices.AsSpan();

    /// <summary>
    /// Copies the vertices of this <see cref="PolygonDefinition"/> into <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy this <see cref="PolygonDefinition"/>'s vertices into</param>
    public void CopyTo(Span<Vector2> destination)
    {
        ((Span<Vector2>)Vertices).CopyTo(destination);
    }

    /// <summary>
    /// Attempts to copy the vertices of this <see cref="PolygonDefinition"/> into <paramref name="destination"/> and returns a value that indicates whether the operation succeeded or not
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy this <see cref="PolygonDefinition"/>'s vertices into</param>
    /// <returns>true if the operation was succesful, false otherwise</returns>
    public bool TryCopyTo(Span<Vector2> destination)
    {
        return ((Span<Vector2>)Vertices).TryCopyTo(destination);
    }

    /// <inheritdoc/>
    public IEnumerator<Vector2> GetEnumerator() => ((IEnumerable<Vector2>)Vertices).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    internal bool RefEquals(PolygonDefinition other) => ReferenceEquals(this.Vertices, other.Vertices);
}