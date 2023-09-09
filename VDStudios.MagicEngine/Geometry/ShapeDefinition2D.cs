using System.Collections;
using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents a the definition of an arbitrary shape
/// </summary>
public abstract class ShapeDefinition2D : IReadOnlyList<Vector2>
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
    public ShapeDefinition2D(bool isConvex)
    {
        IsConvex = isConvex;
    }

    /// <summary>
    /// Notifies the shape that there has been an update it may not be aware of
    /// </summary>
    /// <remarks>
    /// Use and implement with care. If using this method becomes necessary, it's likely an indication of a bug. a <see cref="ShapeDefinition2D"/> should be capable of accounting its state for itself
    /// </remarks>
    public virtual void ForceUpdate() => version++;

    /// <summary>
    /// Forces this <see cref="ShapeDefinition2D"/> that it must discard its current vertex list and regenerate a new one, if applicable.
    /// </summary>
    /// <remarks>
    /// If this method is applicable, and the vertices are regenerated, <see cref="Version"/> will increment
    /// </remarks>
    /// <returns><see langword="true"/> if the vertices are regenerated, <see langword="false"/> otherwise</returns>
    public abstract bool ForceRegenerate();

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

    /// <summary>
    /// Calculates how long a span of <see cref="uint"/> indices need to be for a triangulation operation on this object
    /// </summary>
    /// <returns></returns>
    public abstract int GetTriangulationLength(ElementSkip vertexSkip = default);

    /// <summary>
    /// Triangulates this <see cref="ShapeDefinition2D"/> into a set of indices that run through its vertices as a set of triangles
    /// </summary>
    /// <param name="outputIndices">The output of the operation. See <see cref="GetTriangulationLength"/></param>
    /// <param name="vertexSkip">The amount of vertices to skip when triangulating</param>
    /// <returns>The amount of indices written to <paramref name="outputIndices"/></returns>
    public abstract int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default);

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