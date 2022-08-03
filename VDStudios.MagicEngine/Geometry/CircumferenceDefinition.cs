using System.Numerics;
using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents a Circumference's radius
/// </summary>
public readonly struct Radius
{
    /// <summary>
    /// The radius of the circumference
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// Radius times two (r*2)
    /// </summary>
    public float Diameter => Value * 2;

    /// <summary>
    /// Creates a new instance of <see cref="Radius"/> using a diameter value
    /// </summary>
    /// <returns></returns>
    public static Radius ByDiameter(float value) => new(value / 2);

    /// <summary>
    /// Creates a new instance of <see cref="Radius"/>
    /// </summary>
    private Radius(float value) => Value = value;

    /// <summary>
    /// Implicitly casts an instance of <see cref="float"/> to <see cref="Radius"/>
    /// </summary>
    public static implicit operator float(Radius r) => r.Value;
    
    /// <summary>
    /// Implicitly casts an instance of <see cref="Radius"/> to <see cref="float"/>
    /// </summary>
    public static implicit operator Radius(float r) => new(r);
}

/// <summary>
/// Represents the definition of a single circumference
/// </summary>
public class CircumferenceDefinition : ShapeDefinition
{
    private Vector2[] ___vertexBuffer = Array.Empty<Vector2>();
    private bool ___regenRequired = true;

    private Vector2[] VertexBuffer
    {
        get
        {
            if (___regenRequired)
            {
                GenerateVertices();
                ___regenRequired = false;
            }
            return ___vertexBuffer;
        }
    }

    /// <summary>
    /// The center point of the circumference
    /// </summary>
    public Vector2 CenterPoint { get; }

    /// <summary>
    /// The radius of the circumference
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
            if (subdiv < 3)
                throw new ArgumentException("A Circumference's subdivision count cannot be less than 3", nameof(value));
            subdiv = value;
            ___regenRequired = true;
        }
    }
    private int subdiv;

    /// <inheritdoc/>
    public override int Count => Subdivisions;

    /// <inheritdoc/>
    public override Vector2 this[int index] => VertexBuffer[index];

    /// <summary>
    /// Instances a new object of type <see cref="CircumferenceDefinition"/>
    /// </summary>
    /// <param name="centerPoint">The center point of the circumference</param>
    /// <param name="radius">The length of each point along the circumference from its center, or half its diameter</param>
    /// <param name="subdivisions">The amount of vertices the circumference will have. Must be larger than 3</param>
    public CircumferenceDefinition(Vector2 centerPoint, Radius radius, int subdivisions = 30) : base(true)
    {
        CenterPoint = centerPoint;
        Radius = radius;
        Subdivisions = subdivisions;
    }

    private void GenerateVertices()
    {
        var pbuf = CenterPoint with { X = CenterPoint.X + Radius };
        var rot = Matrix3x2.CreateRotation(MathF.Tau / Subdivisions, CenterPoint);

        var vertices = new Vector2[Subdivisions];
        for (int i = 0; i < Subdivisions; i++)
        {
            vertices[i] = pbuf;
            pbuf = Vector2.Transform(pbuf, rot);
        }
        ___vertexBuffer = vertices;
    }
    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start, int length)
        => VertexBuffer.AsSpan(start, length);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan(int start)
        => VertexBuffer.AsSpan(start);

    /// <inheritdoc/>
    public override ReadOnlySpan<Vector2> AsSpan()
        => VertexBuffer.AsSpan();

    /// <inheritdoc/>
    public override void CopyTo(Span<Vector2> destination)
    {
        ((Span<Vector2>)VertexBuffer).CopyTo(destination);
    }

    /// <inheritdoc/>
    public override IEnumerator<Vector2> GetEnumerator() => ((IEnumerable<Vector2>)VertexBuffer).GetEnumerator();

    /// <inheritdoc/>
    public override Vector2[] ToArray()
    {
        var ret = new Vector2[VertexBuffer.Length];
        VertexBuffer.CopyTo(ret, 0);
        return ret;
    }

    /// <inheritdoc/>
    public override bool TryCopyTo(Span<Vector2> destination)
    {
        return ((Span<Vector2>)VertexBuffer).TryCopyTo(destination);
    }
}
