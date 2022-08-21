using System.Collections;
using System.Numerics;
using VDStudios.MagicEngine.DrawLibrary.Geometry;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents a the definition of an arbitrary shape
/// </summary>
public abstract class ShapeDefinition : IReadOnlyList<Vector2>
{
    /// <summary>
    /// Used for concurrency purposes, and to query for changes
    /// </summary>
    protected int version = 0;

    /// <summary>
    /// Used for concurrency purposes, and to query for changes
    /// </summary>
    public int Version => version;

    /// <summary>
    /// Instances a new object of type ShapeDefinition
    /// </summary>
    /// <param name="isConvex"></param>
    public ShapeDefinition(bool isConvex)
    {
        IsConvex = isConvex;
    }

    /// <summary>
    /// Notifies the shape that there has been an update it may not be aware of. This can be useful to, for example, force a <see cref="ShapeRenderer{TVertex}"/> to regenerate vertices
    /// </summary>
    public virtual void ForceUpdate() => Interlocked.Increment(ref version);

    /// <summary>
    /// A name given to this <see cref="PolygonDefinition"/> for debugging purposes
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether or not the Polygon represented by this <see cref="PolygonDefinition"/> is convex
    /// </summary>
    public bool IsConvex { get; private set; }

    /// <summary>
    /// Creates a new Span over the portion of this <see cref="PolygonDefinition"/> beginning at <paramref name="start"/> for <paramref name="length"/>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public abstract ReadOnlySpan<Vector2> AsSpan(int start, int length);

    /// <summary>
    /// Creates a new Span over the portion of this <see cref="PolygonDefinition"/> beginning at <paramref name="start"/> for the rest of this <see cref="PolygonDefinition"/>
    /// </summary>
    /// <param name="start"></param>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public abstract ReadOnlySpan<Vector2> AsSpan(int start);

    /// <summary>
    /// Creates a new Span over this <see cref="PolygonDefinition"/>
    /// </summary>
    /// <returns>The span representation of this <see cref="PolygonDefinition"/></returns>
    public abstract ReadOnlySpan<Vector2> AsSpan();

    /// <summary>
    /// Copies the vertices of this <see cref="PolygonDefinition"/> into <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy this <see cref="PolygonDefinition"/>'s vertices into</param>
    public abstract void CopyTo(Span<Vector2> destination);

    /// <inheritdoc/>
    public abstract IEnumerator<Vector2> GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Copies the vertices of this <see cref="PolygonDefinition"/> into a new array
    /// </summary>
    /// <returns>The newly created and now filled array</returns>
    public abstract Vector2[] ToArray();

    /// <summary>
    /// Attempts to copy the vertices of this <see cref="PolygonDefinition"/> into <paramref name="destination"/> and returns a value that indicates whether the operation succeeded or not
    /// </summary>
    /// <param name="destination">The <see cref="Span{T}"/> to copy this <see cref="PolygonDefinition"/>'s vertices into</param>
    /// <returns>true if the operation was succesful, false otherwise</returns>
    public abstract bool TryCopyTo(Span<Vector2> destination);

    /// <summary>
    /// Indexes the shape for a specific vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public abstract Vector2 this[int index] { get; }

    /// <summary>
    /// The amount of vertices this Shape has
    /// </summary>
    public abstract int Count { get; }
}