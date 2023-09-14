using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Vertex information containing a 2D polygon position vertex, a 2D texture position vertex and a RGBA color
/// </summary>
public readonly struct VertexTextureColor2D : IVertexType<VertexTextureColor2D>
{
    /// <summary>
    /// The position of the vertex in the polygon
    /// </summary>
    public readonly Vector2 PolygonVertex;

    /// <summary>
    /// The position of the vertex in the texture
    /// </summary>
    public readonly Vector2 TextureVertex;

    /// <summary>
    /// The color of the vertex
    /// </summary>
    public readonly RgbaVector Color;

    /// <summary>
    /// Creates a new <see cref="VertexTextureColor2D"/>
    /// </summary>
    /// <param name="vertex">The position of the vertex in the polygon</param>
    /// <param name="color">The color of the vertex</param>
    /// <param name="texturePosition">The position of the vertex in the texture</param>
    public VertexTextureColor2D(Vector2 vertex, Vector2 texturePosition, RgbaVector color)
    {
        PolygonVertex = vertex;
        Color = color;
        TextureVertex = texturePosition;
    }

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<VertexTextureColor2D>();

    /// <inheritdoc/>
    public static VertexLayoutDescription GetDescription()
        => new(
               new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
               new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
               new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
           );

    /// <inheritdoc/>
    public bool Equals(VertexTextureColor2D other)
        => PolygonVertex == other.PolygonVertex && TextureVertex == other.TextureVertex && Color == other.Color;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(PolygonVertex, TextureVertex, Color);

    /// <inheritdoc/>
    public override bool Equals(object? obj) 
        => obj is VertexTextureColor2D o && Equals(o);

    /// <inheritdoc/>
    public static bool operator ==(VertexTextureColor2D left, VertexTextureColor2D right) 
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(VertexTextureColor2D left, VertexTextureColor2D right) 
        => !(left == right);
}
