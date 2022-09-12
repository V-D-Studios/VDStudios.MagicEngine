using System.Numerics;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents the definition of a single circle
/// </summary>
public class CircleDefinition : ShapeDefinition2D
{
    private Vector2[] ___vertexBuffer = Array.Empty<Vector2>();
    private bool ___regenRequired = true;

    private Span<Vector2> VertexBuffer
    {
        get
        {
            if (___regenRequired)
            {
                if (___vertexBuffer.Length < Subdivisions)
                    ___vertexBuffer = new Vector2[Subdivisions];
                GenerateVertices(CenterPoint, Radius, Subdivisions, ___vertexBuffer.AsSpan(0, Subdivisions));
                ___regenRequired = false;
            }
            return ___vertexBuffer.AsSpan(0, Subdivisions);
        }
    }

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
    /// The radius of the circle
    /// </summary>
    public Radius Radius { get; }

    /// <summary>
    /// The amount of subdivisions the produced polygon will have
    /// </summary>
    public int Subdivisions
    {
        get => subdiv;
        set
        {
            if (value < 3)
                throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(value));
            subdiv = value;
            ___regenRequired = true;
            version++;
        }
    }
    private int subdiv;

    /// <inheritdoc/>
    public override int Count => Subdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <summary>
    /// Instances a new object of type <see cref="CircleDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the circle</param>
    /// <param name="radius">The length of each point along the circle from its center, or half its diameter</param>
    /// <param name="subdivisions">The amount of vertices the circle will have. Must be larger than 3</param>
    public CircleDefinition(Vector2 centerPoint, Radius radius, int subdivisions = 30) : base(true)
    {
        CenterPoint = centerPoint;
        Radius = radius;
        Subdivisions = subdivisions;
    }

    /// <summary>
    /// Generates a list of vertices using the given information
    /// </summary>
    /// <remarks>
    /// This is the method used internally by <see cref="CircleDefinition"/>, can be used to generate vertices into an external buffer separately from a <see cref="CircleDefinition"/> instance. To do it relative to an instance, consider using <see cref="CopyTo(Span{Vector2})"/> instead
    /// </remarks>
    /// <param name="center">The centerpoint of the circle</param>
    /// <param name="radius">The radius of the circle</param>
    /// <param name="subdivisions">The amount of vertices to subdivide the circle into</param>
    /// <param name="buffer">The location in memory into which to store the newly generated vertices</param>
    public static void GenerateVertices(Vector2 center, Radius radius, int subdivisions, Span<Vector2> buffer)
    {
        var pbuf = new Vector2(center.X - radius, center.Y);
        var rot = Matrix3x2.CreateRotation(-MathF.Tau / subdivisions, center);

        for (int i = 0; i < subdivisions; i++)
        {
            buffer[i] = pbuf;
            pbuf = Vector2.Transform(pbuf, rot);
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
}
