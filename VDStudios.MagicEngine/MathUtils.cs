using System.Numerics;
using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine;

/// <summary>
/// Contains a set of assorted math-related helper methods
/// </summary>
public static class MathUtils
{
    /// <summary>
    /// Checks if <paramref name="a"/> and <paramref name="b"/> are equal to each other within <paramref name="epsilon"/>
    /// </summary>
    /// <typeparam name="TNumber"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual<TNumber>(TNumber a, TNumber b, TNumber? epsilon = default) where TNumber : IFloatingPointIeee754<TNumber>
    {
        var eps = epsilon ?? TNumber.Epsilon;
        var MinNormal = TNumber.CreateTruncating(2.2250738585072014E-308d);
        var absA = TNumber.Abs(a);
        var absB = TNumber.Abs(b);
        var diff = TNumber.Abs(a - b);

        return a.Equals(b) || (a == TNumber.Zero || b == TNumber.Zero || absA + absB < MinNormal
                                ? diff < (eps * MinNormal)
                                : diff / (absA + absB) < eps);
    }
}
