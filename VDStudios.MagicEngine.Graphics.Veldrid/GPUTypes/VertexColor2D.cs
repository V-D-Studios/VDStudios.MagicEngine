using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Vertex information containing a 2D polygon position vertex and a RGBA color
/// </summary>
public readonly struct VertexColor2D : IVertexType<VertexColor2D>,
    IDefaultVertexGenerator<Vector2, VertexColor2D>
{
    /// <summary>
    /// The position of the vertex in the polygon
    /// </summary>
    public readonly Vector2 PolygonVertex;

    /// <summary>
    /// The color of the vertex
    /// </summary>
    public readonly RgbaVector Color;

    /// <summary>
    /// Creates a new <see cref="VertexColor2D"/>
    /// </summary>
    /// <param name="vertex">The position of the vertex in the polygon</param>
    /// <param name="color">The color of the vertex</param>
    public VertexColor2D(Vector2 vertex, RgbaVector color)
    {
        PolygonVertex = vertex;
        Color = color;
    }

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<VertexColor2D>();

    /// <inheritdoc/>
    public static VertexLayoutDescription GetDescription()
        => new(
               new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
               new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
           );

    static IVertexGenerator<Vector2, VertexColor2D> IDefaultVertexGenerator<Vector2, VertexColor2D>.DefaultGenerator
        => DefaultGenerator;

    /// <summary>
    /// The default <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> for this type
    /// </summary>
    public static Vector2ToVertexColor2DGen DefaultGenerator => Vector2ToVertexColor2DGen.Default;

    /// <inheritdoc/>
    public bool Equals(VertexColor2D other)
        => PolygonVertex == other.PolygonVertex && Color == other.Color;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(PolygonVertex, Color);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is VertexColor2D o && Equals(o);

    /// <inheritdoc/>
    public static bool operator ==(VertexColor2D left, VertexColor2D right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(VertexColor2D left, VertexColor2D right)
        => !(left == right);
}
