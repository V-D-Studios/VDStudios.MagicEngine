using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a donut shape: A larger circle with a smaller circle inside, the area between the two representing the area of the shape, excluding the inner circle's area
/// </summary>
/// <remarks>
/// The vertices for this <see cref="DonutDefinition"/> are stored first coming the vertices of the inner circle, followed by the vertices of the outer circle. 
/// </remarks>
public class DonutDefinition : ShapeDefinition2D
{
    private readonly Vector2[] ___vertexBuffer;

    private Span<Vector2> VertexBuffer
        => ___vertexBuffer;

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

    private Span<Vector2> SliceInnerCircle(Span<Vector2> buffer) => buffer.Slice(InnerCircleStart, OuterCircleStart);
    private Span<Vector2> SliceOuterCircle(Span<Vector2> buffer) => buffer.Slice(OuterCircleStart, Count - OuterCircleStart);

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
    public Vector2 CenterPoint { get; }

    /// <summary>
    /// The radius of the outer circle
    /// </summary>
    public Radius OuterRadius { get; }

    /// <summary>
    /// The radius of the inner circle
    /// </summary>
    public Radius InnerRadius { get; }

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int OuterSubdivisions { get; }

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int InnerSubdivisions { get; }

    /// <inheritdoc/>
    public override int Count => InnerSubdivisions + OuterSubdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

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

        if (OuterRadius <= InnerRadius) 
            throw new ArgumentException("The outer circle's radius cannot be smaller than or equal to the inner circle's radius", nameof(outerRadius));
        if (OuterSubdivisions < 3)
            throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(outerSubdivisions));
        if (InnerSubdivisions < 3)
            throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(innerSubdivisions));

        ___vertexBuffer = new Vector2[Count];

        var inner = SliceInnerCircle(___vertexBuffer);
        var outer = SliceOuterCircle(___vertexBuffer);

        CircleDefinition.GenerateVertices(CenterPoint, InnerRadius, InnerSubdivisions, inner);
        CircleDefinition.GenerateVertices(CenterPoint, OuterRadius, OuterSubdivisions, outer);
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
    {
        var isp = InnerCircleSpan.Length;
        var osp = OuterCircleSpan.Length;

        return isp == osp
            ? isp * 7
            : throw new NotSupportedException("Triangulating Donut shapes with mismatching inner and outer circle spans is not supported");
    }

    /// <inheritdoc/>
    public override int Triangulate(Span<uint> outputIndices, ElementSkip vertexSkip = default)
    {
        var isp = InnerCircleSpan.Length;
        var osp = OuterCircleSpan.Length;
        var istart = checked((ushort)InnerCircleStart);
        var ostart = checked((ushort)OuterCircleStart);
        int bufind = 0;

        if (isp == osp)
        {
            for (ushort i = 0; i < osp; i++)
            {
                ushort in1 = (ushort)((i + 1) % osp + istart);
                ushort ou0 = (ushort)(i + ostart);

                outputIndices[bufind++] = in1;
                outputIndices[bufind++] = ou0;
                outputIndices[bufind++] = (ushort)(i + istart);

                outputIndices[bufind++] = in1;
                outputIndices[bufind++] = ou0;
                outputIndices[bufind++] = (ushort)((i + 1) % osp + ostart);

                outputIndices[bufind++] = in1;
            }
            return bufind;
        }

        if (isp > osp)
            (isp, osp, istart, ostart) = (osp, isp, ostart, istart); // We swap these variables around so that we only have to write one implementation; and the functionality will be the same

        // This algorithm works under the asumption that there are more subdivisions in the outer circle than in the inner one
        // Therefore, each inner circle vertex will have more than one outer circle vertex connected to it
        ushort ratio = (ushort)(osp / isp);
        for (ushort stage = 0; stage < isp * ratio; stage += ratio)
        {
            ushort in0 = (ushort)(stage / ratio + istart);
            ushort ou1;
            ushort ou0;
            for (ushort i = 0; i < ratio; i++)
            {
                //outputIndices[bufind++] = (ushort)((i + 1) % isp + istart);
                //outputIndices[bufind++] = (ushort)((i % ratio) * stage + ostart);
                //outputIndices[bufind++] = (ushort)(i % isp + istart);

                ou1 = (ushort)(((i + stage) + 1) % osp + ostart);
                ou0 = (ushort)((i + stage) + ostart);

                outputIndices[bufind++] = in0;
                outputIndices[bufind++] = ou1;
                outputIndices[bufind++] = ou0;
            }
            outputIndices[bufind++] = in0;
            outputIndices[bufind++] = (ushort)((in0 + 1) % isp);
            outputIndices[bufind++] = (ushort)((stage + 2) % osp + ostart);
            outputIndices[bufind++] = in0;
        }
        outputIndices[bufind++] = 0;

        return bufind;
    }
}
