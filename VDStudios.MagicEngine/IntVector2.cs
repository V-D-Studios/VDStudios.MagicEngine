using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a vector with two 32 bit signed integer values
/// </summary>
/// <remarks><format type="text/markdown"><![CDATA[
/// The <xref:System.Numerics.Vector2> structure provides support for hardware acceleration.
/// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
/// ]]></format></remarks>
public struct IntVector2 : IEquatable<IntVector2>, IFormattable
{
    /// <summary>
    /// The X component of this vector
    /// </summary>
    public int X;

    /// <summary>
    /// The Y component of this vector
    /// </summary>
    public int Y;

    /// <summary>
    /// Creates a new <see cref="IntVector2"/>
    /// </summary>
    /// <param name="x">The X component of the vector</param>
    /// <param name="y">The Y component of the vector</param>
    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc/>
    public readonly unsafe bool Equals(IntVector2 other)
    {
        return Vector64.IsHardwareAccelerated
            ? Vector64.LoadUnsafe(ref Unsafe.AsRef(in X)).Equals(Vector64.LoadUnsafe(ref other.X))
            : SoftwareFallback(in this, other);

        static bool SoftwareFallback(in IntVector2 self, IntVector2 other) => self.X.Equals(other.X) && self.Y.Equals(other.Y);
    }

    /// <summary>
    /// Deconstructs the current <see cref="IntVector2"/>
    /// </summary>
    /// <param name="x">The X component of the vector</param>
    /// <param name="y">The Y component of the vector</param>
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is IntVector2 vector && Equals(vector);

    /// <inheritdoc/>
    public static bool operator ==(IntVector2 left, IntVector2 right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(IntVector2 left, IntVector2 right)
        => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Vector64.LoadUnsafe(ref X).GetHashCode();

    /// <inheritdoc/>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        => $"<{X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}>";

    /// <inheritdoc/>
    public override readonly string ToString()
        => ToString("G", CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Creates a <see cref="Vector2"/> from this <see cref="IntVector2"/>
    /// </summary>
    /// <returns></returns>
    public readonly Vector2 ToVector2() => new(X, Y);
}
