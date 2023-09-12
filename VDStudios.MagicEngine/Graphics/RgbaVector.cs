using System.Numerics;
using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A color stored in four 32-bit floating-point values, in RGBA component order.
/// </summary>
/// <remarks>
/// Every channel is considered to be in a scale of 0 to 1
/// </remarks>
public struct RgbaVector : IEquatable<RgbaVector>
{
    private Vector4 _channels;

    /// <summary>
    /// Creates a new <see cref="RgbaVector"/> from the presented values
    /// </summary>
    /// <param name="r">The red channel of the vector</param>
    /// <param name="g">The green channel of the vector</param>
    /// <param name="b">The blue channel of the vector</param>
    /// <param name="a">The alpha (opacity) channel of the vector</param>
    public RgbaVector(float r, float g, float b, float a) : this()
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a <see cref="RgbaVector"/> object from the presented <see cref="Vector4"/>
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
    public RgbaVector(Vector4 channels)
    {
        _channels = channels;
    }

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
    /// Returns a string representation of this color.
    /// </summary>
    /// <returns></returns>
    public override readonly string ToString()
        => string.Format("R:{0}, G:{1}, B:{2}, A:{3}", R, G, B, A);

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
    public readonly Vector4 ToVector4() => _channels;

    /// <summary>
    /// Red (1, 0, 0, 1)
    /// </summary>
    public static readonly RgbaVector Red = new(1, 0, 0, 1);

    /// <summary>
    /// Dark Red (0.6f, 0, 0, 1)
    /// </summary>
    public static readonly RgbaVector DarkRed = new(0.6f, 0, 0, 1);

    /// <summary>
    /// Green (0, 1, 0, 1)
    /// </summary>
    public static readonly RgbaVector Green = new(0, 1, 0, 1);

    /// <summary>
    /// Blue (0, 0, 1, 1)
    /// </summary>
    public static readonly RgbaVector Blue = new(0, 0, 1, 1);

    /// <summary>
    /// Yellow (1, 1, 0, 1)
    /// </summary>
    public static readonly RgbaVector Yellow = new(1, 1, 0, 1);

    /// <summary>
    /// Grey (0.25f, 0.25f, 0.25f, 1)
    /// </summary>
    public static readonly RgbaVector Grey = new(.25f, .25f, .25f, 1);

    /// <summary>
    /// Light Grey (0.65f, 0.65f, 0.65f, 1)
    /// </summary>
    public static readonly RgbaVector LightGrey = new(.65f, .65f, .65f, 1);

    /// <summary>
    /// Cyan (0, 1, 1, 1)
    /// </summary>
    public static readonly RgbaVector Cyan = new(0, 1, 1, 1);

    /// <summary>
    /// White (1, 1, 1, 1)
    /// </summary>
    public static readonly RgbaVector White = new(1, 1, 1, 1);

    /// <summary>
    /// Cornflower Blue (0.3921f, 0.5843f, 0.9294f, 1)
    /// </summary>
    public static readonly RgbaVector CornflowerBlue = new(0.3921f, 0.5843f, 0.9294f, 1);

    /// <summary>
    /// Clear (0, 0, 0, 0)
    /// </summary>
    public static readonly RgbaVector Clear = new(0, 0, 0, 0);

    /// <summary>
    /// Black (0, 0, 0, 1)
    /// </summary>
    public static readonly RgbaVector Black = new(0, 0, 0, 1);

    /// <summary>
    /// Pink (1, 0.45f, 0.75f, 1)
    /// </summary>
    public static readonly RgbaVector Pink = new(1f, 0.45f, 0.75f, 1);

    /// <summary>
    /// Orange (1, 0.36f, 0, 1)
    /// </summary>
    public static readonly RgbaVector Orange = new(1f, 0.36f, 0f, 1);
}
