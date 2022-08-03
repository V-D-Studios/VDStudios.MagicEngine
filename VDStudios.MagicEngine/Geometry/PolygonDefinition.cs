using SDL2.NET;
using System;
using System.Collections;
using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single polygon, starting from the furthest to the bottom and to the left, in CW order
/// </summary>
/// <remarks>
/// Vertices should be defined in a space relative to themselves, as transformations and positions should be handled by the owner of the definition
/// </remarks>
public sealed class PolygonDefinition : IReadOnlyList<Vector2>, IStructuralEquatable
{
    #region Predefined Polygons

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Circle
    /// </summary>
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

        Span<Vector2> vertices = stackalloc Vector2[subdivisions];
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
    public static PolygonDefinition Rectangle(FRectangle rectangle)
    {
        var (w, h, x, y) = rectangle;
        return Rectangle(new(x, y), new(w, h));
    }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> object that describes a Rectangle represented by <paramref name="rectangle"/>
    /// </summary>
    /// <param name="rectangle">The rectangle describing the location and dimensions of the polygon to define</param>
    /// <returns>A new <see cref="PolygonDefinition"/> with four vertices describing the rectangle</returns>
    public static PolygonDefinition Rectangle(Rectangle rectangle)
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

    #endregion

    private readonly Vector2[] Vertices;

    /// <summary>
    /// A name given to this <see cref="PolygonDefinition"/> for debugging purposes
    /// </summary>
    public string? Name { get; set; }

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
    /// Whether or not the Polygon represented by this <see cref="PolygonDefinition"/> is convex
    /// </summary>
    public bool IsConvex { get; private set; }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/> up until <paramref name="vertexCount"/>, or until the length of <paramref name="vertices"/> if it's a negative number
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    /// <param name="vertexCount">The amount of vertices to take from <paramref name="vertices"/>. <c>0</c> represents all indices, negative numbers start from the end</param>
    public PolygonDefinition(Vector2[] vertices, bool isConvex, int vertexCount = 0)
        : this(vertices.AsSpan(0, vertexCount is 0 ? vertices.Length : vertexCount > 0 ? Index.FromStart(vertexCount).GetOffset(vertices.Length) : Index.FromEnd(vertexCount).GetOffset(vertices.Length)), isConvex) { }

    /// <summary>
    /// Creates a new <see cref="PolygonDefinition"/> with the vectors provided in <paramref name="vertices"/>
    /// </summary>
    /// <param name="vertices">The vertices of the polygon</param>
    public PolygonDefinition(ReadOnlySpan<Vector2> vertices, bool isConvex)
    {
        if (vertices.Length is < 3)
            throw new ArgumentException("A polygon must have at least 3 vertices", nameof(vertices));
        IsConvex = isConvex;
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
}