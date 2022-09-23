using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a donut shape: A larger circle with a smaller circle inside, the area between the two representing the area of the shape, excluding the inner circle's area
/// </summary>
/// <remarks>
/// The vertices for this <see cref="DonutDefinition"/> are stored first coming the vertices of the inner circle, followed by the vertices of the outer circle. This shape is special, and is only triangulated properly because <see cref="DrawLibrary.Geometry.Shape2DTriangulatedIndexGenerator"/> performs a special check!
/// </remarks>
public class DonutDefinition : ShapeDefinition2D
{
    private Vector2[] ___vertexBuffer = Array.Empty<Vector2>();
    private bool ___regenRequired = true;

    private Span<Vector2> VertexBuffer
    {
        get
        {
            if (___regenRequired)
            {
                if (___vertexBuffer.Length < Count)
                    ___vertexBuffer = new Vector2[Count];
                CircleDefinition.GenerateVertices(CenterPoint, InnerRadius, InnerSubdivisions, SliceInnerCircle(___vertexBuffer));
                CircleDefinition.GenerateVertices(CenterPoint, OuterRadius, OuterSubdivisions, SliceOuterCircle(___vertexBuffer));
                ___regenRequired = false;
            }
            return ___vertexBuffer.AsSpan(0, Count);
        }
    }

    /// <summary>
    /// The index at which the outer circle's vertices start
    /// </summary>
    /// <remarks>
    /// The outer circle's vertices continue from this index to the end of the array
    /// </remarks>
    public int OuterCircleStart => InnerSubdivisions;

    /// <summary>
    /// The index at which the inner circle's vertices start
    /// </summary>
    /// <remarks>
    /// The inner circle's vertices continue from this index up to (inclusive) <c><see cref="OuterCircleStart"/> - 1</c>
    /// </remarks>
    public int InnerCircleStart => 0;

    private Span<Vector2> SliceInnerCircle(Span<Vector2> buffer) => buffer.Slice(InnerCircleStart, OuterCircleStart - 1);
    private Span<Vector2> SliceOuterCircle(Span<Vector2> buffer) => buffer.Slice(OuterCircleStart, Count);

    /// <summary>
    /// Represents the area in this <see cref="DonutDefinition"/>'s vertex array that contains the Inner Circle's vertices
    /// </summary>
    public ReadOnlySpan<Vector2> InnerCircleSpan => SliceInnerCircle(VertexBuffer);

    /// <summary>
    /// Represents the area in this <see cref="DonutDefinition"/>'s vertex array that contains the Outer Circle's vertices
    /// </summary>
    public ReadOnlySpan<Vector2> OuterCircleSpan => SliceOuterCircle(VertexBuffer);

    /// <summary>
    /// The center point of the circle
    /// </summary>
    public Vector2 CenterPoint
    {
        get => cenp;
        set
        {
            if (value == cenp)
                return;
            cenp = value;
            ___regenRequired = true;
            version++;
        }
    }
    private Vector2 cenp;

    /// <summary>
    /// The radius of the outer circle
    /// </summary>
    public Radius OuterRadius
    {
        get => orad;
        set
        {
            if (value == orad) return;
            if (value <= irad) throw new ArgumentException("The outer circle's radius cannot be smaller than or equal to the inner circle's radius", nameof(value));
            ___regenRequired = true;
            orad = value;
        }
    }
    private Radius orad;

    /// <summary>
    /// The radius of the inner circle
    /// </summary>
    public Radius InnerRadius
    {
        get => irad;
        set
        {
            if (value == irad) return;
            if (value >= orad) throw new ArgumentException("The inner circle's radius cannot be larger than or equal to the outer circle's radius", nameof(value));
            ___regenRequired = true;
            irad = value;
        }
    }
    private Radius irad;

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int OuterSubdivisions
    {
        get => osubdiv;
        set
        {
            if (value < 3)
                throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(value));
            osubdiv = value;
            ___regenRequired = true;
            version++;
        }
    }
    private int osubdiv;

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int InnerSubdivisions
    {
        get => isubdiv;
        set
        {
            if (value < 3)
                throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(value));
            isubdiv = value;
            ___regenRequired = true;
            version++;
        }
    }
    private int isubdiv;

    /// <inheritdoc/>
    public override int Count => InnerSubdivisions + OuterSubdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <inheritdoc/>
    public override bool ForceRegenerate()
    {
        ___regenRequired = true;
        ForceUpdate();
        return true;
    }

    /// <summary>
    /// Instances a new object of type <see cref="DonutDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the donut</param>
    /// <param name="outerRadius">The length of each point along the outer circle from its center, or half its diameter. Must be larger than <paramref name="innerRadius"/></param>
    /// <param name="innerRadius">The length of each point along the inner circle from its center, or half its diameter. Must be lesser than <paramref name="outerRadius"/></param>
    /// <param name="outerSubdivisions">The amount of vertices the outer circle will have. Must be larger than 3</param>
    /// <param name="innerSubdivisions">The amount of vertices the inner circle will have. Must be larger than 3</param>
    public DonutDefinition(Vector2 centerPoint, Radius outerRadius, Radius innerRadius, int outerSubdivisions = 30, int innerSubdivisions = 30) : base(true)
    {
        CenterPoint = centerPoint;
        OuterRadius = outerRadius; // This will always be larger than default(float), innerRadius' default.
        InnerRadius = innerRadius; // OuterRadius will already be set, so the check will work as intended.
        OuterSubdivisions = outerSubdivisions;
        InnerSubdivisions = innerSubdivisions;
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
}
