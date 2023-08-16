using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a vector with 4 <see cref="float"/> values representing <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/> and <see cref="Height"/> of a Rectangle
/// </summary>
/// <remarks><format type="text/markdown"><![CDATA[
/// The <xref:System.Numerics.Vector2> structure provides support for hardware acceleration.
/// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
/// ]]></format></remarks>
public struct FloatRectangle : IEquatable<FloatRectangle>, IFormattable
{
    /// <summary>
    /// The position on the X axis of this rectangle
    /// </summary>
    public float X;

    /// <summary>
    /// The position on the Y axis of this rectangle
    /// </summary>
    public float Y;

    /// <summary>
    /// The width of this rectangle
    /// </summary>
    public float Width;

    /// <summary>
    /// The height of this rectangle
    /// </summary>
    public float Height;

    /// <summary>
    /// The size component of this <see cref="FloatRectangle"/>
    /// </summary>
    public readonly Vector2 Size => new(Width, Height);

    /// <summary>
    /// The position component of this <see cref="FloatRectangle"/>
    /// </summary>
    public readonly Vector2 Position => new(X, Y);

    /// <summary>
    /// Creates a new <see cref="FloatRectangle"/>
    /// </summary>
    /// <param name="x">The position on the X axis of this rectangle</param>
    /// <param name="y">The position on the Y axis of this rectangle</param>
    /// <param name="width">The width of this rectangle</param>
    /// <param name="height">The height of this rectangle</param>
    public FloatRectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a <see cref="FloatRectangle"/> from <paramref name="vector"/>
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <item>
    /// <term>FloatRectangle</term>
    /// <term>Vector4</term>
    /// </item>
    /// </listheader>
    /// <item>
    /// <term><see cref="X"/></term>
    /// <term><see cref="Vector4.X"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Y"/></term>
    /// <term><see cref="Vector4.Y"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Width"/></term>
    /// <term><see cref="Vector4.Z"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Height"/></term>
    /// <term><see cref="Vector4.W"/></term>
    /// </item>
    /// </list>
    /// </remarks>
    public FloatRectangle(Vector4 vector) : this(vector.X, vector.Y, vector.Z, vector.W) { }

    /// <inheritdoc/>
    public readonly bool Equals(FloatRectangle other)
        => ToVector4().Equals(other.ToVector4());

    /// <summary>
    /// Deconstructs the current <see cref="FloatRectangle"/>
    /// </summary>
    /// <param name="x">The position on the X axis of this rectangle</param>
    /// <param name="y">The position on the Y axis of this rectangle</param>
    /// <param name="width">The width of this rectangle</param>
    /// <param name="height">The height of this rectangle</param>
    public readonly void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is FloatRectangle vector && Equals(vector);

    /// <inheritdoc/>
    public static bool operator ==(FloatRectangle left, FloatRectangle right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(FloatRectangle left, FloatRectangle right)
        => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Vector64.LoadUnsafe(ref X).GetHashCode();

    /// <inheritdoc/>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        => $"<X: {X.ToString(format, formatProvider)}, Y: {Y.ToString(format, formatProvider)}, Width: {Width.ToString(format, formatProvider)}, Height: {Height.ToString(format, formatProvider)}>";

    /// <inheritdoc/>
    public override readonly string ToString()
        => ToString("G", CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Whether or not two <see cref="FloatRectangle"/>s intersect
    /// </summary>
    /// <remarks>
    /// Two rectangles intersect when one is entirely contained within another, are the same rectangle, or any part of a rectangle crosses into the area covered by the other
    /// </remarks>
    public static bool Intersects(FloatRectangle a, FloatRectangle b)
        => a == b || float.Abs(a.X - b.X) < float.Abs(a.Width + b.Width) / 2 && float.Abs(a.Y - b.Y) < float.Abs(a.Height + b.Height) / 2;

    /// <summary>
    /// Whether or not <paramref name="container"/> completely contains <paramref name="contained"/>
    /// </summary>
    /// <param name="container">The <see cref="FloatRectangle"/> that supposedly contains <paramref name="contained"/> entirely</param>
    /// <param name="contained">The <see cref="FloatRectangle"/> that is supposedly entirely contained by <paramref name="container"/></param>
    /// <remarks>
    /// A rectangle contains another when both are the same rectangle, or the contained's X and Y components are both more than or equal to its container's X and Y components, and the contained's Width and Height are both less than or equal to its container's Width and Height
    /// </remarks>
    public static bool Contains(FloatRectangle container, FloatRectangle contained)
        => container == contained || contained.X >= container.X && contained.Y >= container.Y && contained.Width <= container.Width && contained.Height <= container.Height;

    /// <summary>
    /// Whether or not <paramref name="point"/> is completely contained by <paramref name="container"/>
    /// </summary>
    /// <param name="container">The <see cref="FloatRectangle"/> that supposedly contains <paramref name="point"/></param>
    /// <param name="point">The point that is supposedly contained by <paramref name="container"/></param>
    /// <remarks>
    /// A point is contained within a Rectangle when the point's X and Y component are both greater than or equal to the container's X and Y components, and less than or equal to the container's Width and Height
    /// </remarks>
    public static bool Contains(FloatRectangle container, Vector2 point)
        => point.X >= container.X && point.Y >= container.Y && point.X <= container.Width && point.Y <= container.Height;

    /// <summary>
    /// Creates a <see cref="Vector4"/> from this <see cref="FloatRectangle"/>
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <item>
    /// <term>FloatRectangle</term>
    /// <term>Vector4</term>
    /// </item>
    /// </listheader>
    /// <item>
    /// <term><see cref="X"/></term>
    /// <term><see cref="Vector4.X"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Y"/></term>
    /// <term><see cref="Vector4.Y"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Width"/></term>
    /// <term><see cref="Vector4.Z"/></term>
    /// </item>
    /// <item>
    /// <term><see cref="Height"/></term>
    /// <term><see cref="Vector4.W"/></term>
    /// </item>
    /// </list>
    /// </remarks>
    /// <returns></returns>
    public readonly Vector4 ToVector4() => new(X, Y, Width, Height);
}