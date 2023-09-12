using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Vertex information containing a 2D polygon position vertex and a RGBA color
/// </summary>
public readonly struct VertexColor2D : IVertexType<VertexColor2D>
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

    public static VertexLayoutDescription GetDescription()
    {
        throw new NotImplementedException();
    }
}