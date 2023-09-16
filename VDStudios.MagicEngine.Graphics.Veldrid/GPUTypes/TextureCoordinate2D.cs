using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Vertex information containing a 2D texture coordinate vertex
/// </summary>
public readonly struct TextureCoordinate2D : IVertexType<TextureCoordinate2D>, IDefaultVertexGenerator<Vector2, TextureCoordinate2D>
{
    /// <summary>
    /// The position of the vertex in the texture
    /// </summary>
    public readonly Vector2 TextureCoordinate;

    /// <summary>
    /// Creates a new instance of type <see cref="TextureCoordinate2D"/>
    /// </summary>
    public TextureCoordinate2D(float x, float y)
    {
        TextureCoordinate = new(x, y);
    }

    /// <summary>
    /// Creates a new instance of type <see cref="TextureCoordinate2D"/>
    /// </summary>
    public TextureCoordinate2D(Vector2 textureCoordinate)
    {
        TextureCoordinate = textureCoordinate;
    }

    /// <inheritdoc/>
    public static VertexLayoutDescription GetDescription()
        => new(
            new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
        );

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<TextureCoordinate2D>();

    /// <inheritdoc/>
    public bool Equals(TextureCoordinate2D other)
        => TextureCoordinate == other.TextureCoordinate;

    /// <inheritdoc/>
    public override int GetHashCode()
        => TextureCoordinate.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is TextureCoordinate2D o && Equals(o);

    /// <inheritdoc/>
    public static bool operator ==(TextureCoordinate2D left, TextureCoordinate2D right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(TextureCoordinate2D left, TextureCoordinate2D right)
        => !(left == right);

    /// <inheritdoc/>
    public static IVertexGenerator<Vector2, TextureCoordinate2D> DefaultGenerator => TextureFill2DVertexGenerator.Default;
}
