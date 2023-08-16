using System.Numerics;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A color stored in four 32-bit floating-point values, in RGBA component order.
/// </summary>
public struct RgbaVector : IEquatable<RgbaVector>
{
    private Vector4 _channels;

    /// <summary>
    /// The red component.
    /// </summary>
    public float R
    {
        readonly get => _channels.X;
        set => _channels.X = value;
    }

    /// <summary>
    /// The green component.
    /// </summary>
    public float G
    {
        readonly get => _channels.Y;
        set => _channels.Y = value;
    }

    /// <summary>
    /// The blue component.
    /// </summary>
    public float B
    {
        readonly get => _channels.Z;
        set => _channels.Z = value;
    }
    /// <summary>
    /// The alpha component.
    /// </summary>
    public float A
    {
        readonly get => _channels.W;
        set => _channels.W = value;
    }

    /// <inheritdoc/>
    public readonly bool Equals(RgbaVector other)
        => _channels.Equals(other);

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is RgbaVector channels && Equals(channels);

    /// <inheritdoc/>
    public static bool operator ==(RgbaVector left, RgbaVector right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(RgbaVector left, RgbaVector right)
        => !(left == right);

    /// <inheritdoc/>
    public override readonly int GetHashCode()
        => _channels.GetHashCode();

    /// <summary>
    /// Creates a <see cref="Vector4"/> object from this <see cref="RgbaVector"/>
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <item>
    /// <term> <see cref="RgbaVector"/> element </term>
    /// <term> <see cref="Vector4"/> element </term>
    /// </item>
    /// </listheader>
    /// <item> <term> <see cref="R"/> </term> <term> <see cref="Vector4.X"/> </term> </item> 
    /// <item> <term> <see cref="G"/> </term> <term> <see cref="Vector4.Y"/> </term> </item>
    /// <item> <term> <see cref="B"/> </term> <term> <see cref="Vector4.Z"/> </term> </item>
    /// <item> <term> <see cref="A"/> </term> <term> <see cref="Vector4.W"/> </term> </item>
    /// </list>
    /// </remarks>
    /// <returns></returns>
    public readonly Vector4 ToVector4() => _channels;
}
