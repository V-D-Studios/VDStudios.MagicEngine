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
    /// The transformation matrix that transforms the texture's coordinates
    /// </summary>
    public Matrix3x2 Transformation { get; }

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<TextureVector2Viewport>();

#warning Not fully implemented

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
