﻿using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a rectangle
/// </summary>
public class RectangleDefinition : ShapeDefinition2D
{
    /// <summary>
    /// The Position of this Rectangle
    /// </summary>
    public Vector2 Position { get; }

    /// <summary>
    /// The size of this Rectangle
    /// </summary>
    public Vector2 Size { get; }

    /// <summary>
    /// Represents this <see cref="RectangleDefinition"/> as a <see cref="FloatRectangle"/>
    /// </summary>
    public FloatRectangle ToFloatRectangle()
        => new(Position, Size);

    private readonly Vector2[] vertices = new Vector2[4];

    /// <summary>
    /// Creates a new RectangleDefinition
    /// </summary>
    public RectangleDefinition(FloatRectangle rectangle) : this(rectangle.Position, rectangle.Size) { }

    /// <summary>
    /// Creates a new RectangleDefinition
    /// </summary>
    public RectangleDefinition(Vector2 position, Vector2 size) : base(true)
    {
        Position = position;
        Size = size;

        vertices = new Vector2[4]
        {
            Position,
            new(Position.X, Position.Y + Size.Y),
            Position + Size,
            new(Position.X + Size.X, Position.Y)
        };
    }

    /// <inheritdoc/>
    public override bool ForceRegenerate()
        => false;

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => vertices.AsSpan(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => vertices.AsSpan(start);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan()
        => vertices.AsSpan();

    /// <inheritdoc/>
    public override void CopyTo(Span<Vector2> destination)
        => vertices.CopyTo(destination);

    /// <inheritdoc/>
    public override IEnumerator<Vector2> GetEnumerator()
        => ((IEnumerable<Vector2>)vertices).GetEnumerator();

    /// <inheritdoc/>
    public override int GetTriangulationLength() => TriangulatedRectangle.Length;

    private readonly uint[] TriangulatedRectangle = { 0, 1, 2, 0, 3, 2, 0 };

    /// <inheritdoc/>
    public override void Triangulate(Span<uint> outputIndices) 
        => TriangulatedRectangle.CopyTo(outputIndices);

    /// <inheritdoc/>
    public override Vector2[] ToArray()
    {
        var a = new Vector2[vertices.Length];
        vertices.CopyTo(a, 0);
        return a;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
        => vertices.AsSpan().TryCopyTo(destination);

    /// <inheritdoc/>
    public override Vector2 this[int index] 
        => vertices[index];

    /// <inheritdoc/>
    public override int Count => vertices.Length;
}
