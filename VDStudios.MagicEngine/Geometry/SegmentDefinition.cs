using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition for a segment that has a point A, a point B and a width
/// </summary>
public class SegmentDefinition : ShapeDefinition
{
    private readonly Vector2[] ___vertices = new Vector2[4];
    private bool ___regenRequired;

    private Span<Vector2> Vertices
    {
        get
        {
            if (___regenRequired)
            {
                ___vertices[0] = PointA * (1 + Width / 100);
                ___vertices[1] = PointA * -(1 + Width / 100);
                ___vertices[2] = PointB * (1 + Width / 100);
                ___vertices[3] = PointB * -(1 + Width / 100);
                ___regenRequired = false;
            }

            return ___vertices;
        }
    }

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
        ___regenRequired = true;
    }


    /// <summary>
    /// The starting point of the segment
    /// </summary>
    public Vector2 PointA { get; set; }

    /// <summary>
    /// The ending point of the segment
    /// </summary>
    public Vector2 PointB { get; set; }

    /// <summary>
    /// The width of the segment
    /// </summary>
    public float Width
    {
        get => __width;
        set
        {
            if (value == __width) return;
            if (__width <= 0f) throw new ArgumentOutOfRangeException(nameof(value), value, "Width cannot be less than 0");
            __width = value;
        }
    }
    private float __width;

#warning Add variable width along the points
#warning Add the option to add an amount of vertices to the end of the lines for smoothing, where 0 would be a flat line, and one would make the segment end with a triangle
#warning Add methods to set all members at once, to reduce unnecessary regenerations

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
    {
        var verts = Vertices;
        int count = verts.Length;
        var buffer = ArrayPool<Vector2>.Shared.Rent(count);
        verts.CopyTo(buffer);
        try
        {
            for (int i = 0; i < count; i++)
                yield return buffer[i];
        }
        finally
        {
            ArrayPool<Vector2>.Shared.Return(buffer);
        }
    }

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
}
