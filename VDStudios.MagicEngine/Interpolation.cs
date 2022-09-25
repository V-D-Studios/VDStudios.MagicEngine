using SixLabors.ImageSharp.Processing.Processors.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Contains a variety of static CPU-bound interpolation methods
/// </summary>
public static class Interpolation
{
    #region Delegates

    /// <summary>
    /// Performs an interpolation between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <typeparam name="TValue">The type of values that will be interpolated</typeparam>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public delegate TValue InterpolationFunction<TValue>(TValue a, TValue b, TValue amount)
        where TValue :
        INumber<TValue>,
        ILogarithmicFunctions<TValue>,
        IExponentialFunctions<TValue>,
        IHyperbolicFunctions<TValue>,
        IPowerFunctions<TValue>,
        IRootFunctions<TValue>,
        ITrigonometricFunctions<TValue>;

    /// <summary>
    /// Performs an interpolation between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <typeparam name="TValue">The type of values that will be interpolated</typeparam>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <param name="output">The buffer wherein to write the output values</param>
    public delegate void VectorInterpolationFunction<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount)
        where TValue :
        INumber<TValue>,
        ILogarithmicFunctions<TValue>,
        IExponentialFunctions<TValue>,
        IHyperbolicFunctions<TValue>,
        IPowerFunctions<TValue>,
        IRootFunctions<TValue>,
        ITrigonometricFunctions<TValue>;

    #endregion

    #region Linear

    /// <summary>
    /// Performs a linear interpolation between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <typeparam name="TValue">The type of values that will be interpolated</typeparam>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue Linear<TValue>(TValue a, TValue b, TValue amount)
        where TValue :
        INumber<TValue>
        => a + amount * (b - a);

    /// <summary>
    /// Performs a linear interpolation between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <typeparam name="TValue">The type of values that will be interpolated</typeparam>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <param name="output">The buffer wherein to write the output values</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Linear<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount)
        where TValue :
        INumber<TValue>
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Parameters 'a' and 'b' are not the same length");
        if (a.Length < output.Length)
            throw new ArgumentException("The output buffer must be at least the length of the inputs");
        for (int i = 0; i < a.Length; i++)
            output[i] = a[i] + amount * (b[i] - a[i]);
    }

    #endregion
}
