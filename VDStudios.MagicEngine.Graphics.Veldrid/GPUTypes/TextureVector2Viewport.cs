using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Information to transform texture coordinate information of a texture that uses <see cref="Vector2"/> coordinates
/// </summary>
public readonly struct TextureVector2Viewport : IGPUType<TextureVector2Viewport>
{
    /// <summary>
    /// Creates a new object of type <see cref="TextureVector2Viewport"/>
    /// </summary>
    public TextureVector2Viewport(Matrix4x4 transformation)
    {
        Transformation = transformation;
    }

    /// <summary>
    /// The transformation matrix that transforms the texture's coordinates
    /// </summary>
    public Matrix4x4 Transformation { get; }

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<TextureVector2Viewport>();

    /// <summary>
    /// Implicitly converts <paramref name="matrix"/> into a <see cref="TextureVector2Viewport"/> of equal value
    /// </summary>
    public static implicit operator TextureVector2Viewport(Matrix4x4 matrix)
        => new(matrix);

    /// <inheritdoc/>
    public bool Equals(TextureVector2Viewport other)
        => Transformation.Equals(other.Transformation);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Transformation.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is TextureVector2Viewport o && Equals(o);

    /// <inheritdoc/>
    public static bool operator ==(TextureVector2Viewport left, TextureVector2Viewport right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(TextureVector2Viewport left, TextureVector2Viewport right)
        => !(left == right);
}
