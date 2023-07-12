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
    public static TValue InterpolateLinear<TValue>(TValue a, TValue b, TValue amount)
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
    public static void InterpolateLinear<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount)
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

#region Classes

/// <summary>
/// Represents an object that can interpolate between two values or set of values
/// </summary>
public interface IInterpolator
{
    /// <summary>
    /// Represents the singleton instance of this <see cref="IInterpolator"/>
    /// </summary>
    public static abstract IInterpolator Interpolator { get; }

    /// <summary>
    /// Performs an interpolation between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <typeparam name="TValue">The type of values that will be interpolated</typeparam>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public TValue Interpolate<TValue>(TValue a, TValue b, TValue amount)
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
    public void VectorInterpolate<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount)
        where TValue :
        INumber<TValue>,
        ILogarithmicFunctions<TValue>,
        IExponentialFunctions<TValue>,
        IHyperbolicFunctions<TValue>,
        IPowerFunctions<TValue>,
        IRootFunctions<TValue>,
        ITrigonometricFunctions<TValue>;

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public unsafe Vector2 Interpolate(Vector2 a, Vector2 b, float amount)
    {
        Vector2 outp;
        VectorInterpolate(
            new Span<float>(&a, sizeof(Vector2) / sizeof(float)),
            new Span<float>(&b, sizeof(Vector2) / sizeof(float)),
            new Span<float>(&outp, sizeof(Vector2) / sizeof(float)),
            amount);
        return outp;
    }

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public unsafe Vector3 Interpolate(Vector3 a, Vector3 b, float amount)
    {
        Vector3 outp;
        VectorInterpolate(
            new Span<float>(&a, sizeof(Vector3) / sizeof(float)),
            new Span<float>(&b, sizeof(Vector3) / sizeof(float)),
            new Span<float>(&outp, sizeof(Vector3) / sizeof(float)),
            amount);
        return outp;
    }

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public unsafe Vector4 Interpolate(Vector4 a, Vector4 b, float amount)
    {
        Vector4 outp;
        VectorInterpolate(
            new Span<float>(&a, sizeof(Vector4) / sizeof(float)),
            new Span<float>(&b, sizeof(Vector4) / sizeof(float)),
            new Span<float>(&outp, sizeof(Vector4) / sizeof(float)),
            amount);
        return outp;
    }

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public unsafe Matrix3x2 Interpolate(Matrix3x2 a, Matrix3x2 b, float amount)
    {
        Matrix3x2 outp;
        VectorInterpolate(
            new Span<float>(&a, sizeof(Matrix3x2) / sizeof(float)),
            new Span<float>(&b, sizeof(Matrix3x2) / sizeof(float)),
            new Span<float>(&outp, sizeof(Matrix3x2) / sizeof(float)),
            amount);
        return outp;
    }

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/>
    /// </summary>
    /// <param name="a">The value to interpolate to <paramref name="b"/></param>
    /// <param name="b">The value to be interpolated to from <paramref name="a"/></param>
    /// <param name="amount">The inteprolation amount between the values</param>
    /// <returns>The interpolated value</returns>
    public unsafe Matrix4x4 Interpolate(Matrix4x4 a, Matrix4x4 b, float amount)
    {
        Matrix4x4 outp;
        VectorInterpolate(
            new Span<float>(&a, sizeof(Matrix4x4) / sizeof(float)),
            new Span<float>(&b, sizeof(Matrix4x4) / sizeof(float)),
            new Span<float>(&outp, sizeof(Matrix4x4) / sizeof(float)),
            amount);
        return outp;
    }
}

/// <summary>
/// An <see cref="IInterpolator"/> that interpolates two values with a blend factor that considers an exponential aspect of an expression
/// </summary>
public sealed class ExponentialInterpolator : IInterpolator
{
    /// <summary>
    /// The interpolation speed when calculating blend
    /// </summary>
    /// <remarks>
    /// <see cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)"/> (<see cref="BlendBase"/>, amount * <b><see cref="Speed"/></b>
    /// </remarks>
    public float Speed { get; set; }

    /// <summary>
    /// The blend base value when interpolating
    /// </summary>
    /// <remarks>
    /// <see cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)"/> (<b><see cref="BlendBase"/></b>, amount * <see cref="Speed"/>
    /// </remarks>
    public float BlendBase { get; set; }

    /// <summary>
    /// Creates a new object of type <see cref="ExponentialInterpolator"/> with the given starting parameters
    /// </summary>
    /// <param name="speed">The interpolation speed when calculating blend</param>
    /// <param name="blendbase">The blend base value when interpolating</param>
    public ExponentialInterpolator(float speed, float blendbase = .5f) { }

    /// <inheritdoc/>
    public static IInterpolator Interpolator { get; } = new ExponentialInterpolator(1);

    /// <summary>
    /// Calculates the exponential blend value for the linear interpolation
    /// </summary>
    /// <typeparam name="TValue">The type of value to be blended</typeparam>
    /// <param name="amount">The interpolation amount</param>
    public TValue CalculateExponentialLerpBlend<TValue>(TValue amount) where TValue : INumber<TValue>, ILogarithmicFunctions<TValue>, IExponentialFunctions<TValue>, IHyperbolicFunctions<TValue>, IPowerFunctions<TValue>, IRootFunctions<TValue>, ITrigonometricFunctions<TValue>
        => TValue.Pow(TValue.CreateTruncating(BlendBase), amount * TValue.CreateTruncating(Speed));

    /// <summary>
    /// Calculates the exponential blend value for the linear interpolation
    /// </summary>
    /// <param name="amount">The interpolation amount</param>
    public float CalculateExponentialLerpBlend(float amount)
        => float.Pow(BlendBase, amount * Speed);

    /// <inheritdoc/>
    public TValue Interpolate<TValue>(TValue a, TValue b, TValue amount) where TValue : INumber<TValue>, ILogarithmicFunctions<TValue>, IExponentialFunctions<TValue>, IHyperbolicFunctions<TValue>, IPowerFunctions<TValue>, IRootFunctions<TValue>, ITrigonometricFunctions<TValue>
        => LinearInterpolator.Instance.Interpolate(a, b, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public void VectorInterpolate<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount) where TValue : INumber<TValue>, ILogarithmicFunctions<TValue>, IExponentialFunctions<TValue>, IHyperbolicFunctions<TValue>, IPowerFunctions<TValue>, IRootFunctions<TValue>, ITrigonometricFunctions<TValue>
        => LinearInterpolator.Instance.VectorInterpolate(a, b, output, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public Vector2 Interpolate(Vector2 a, Vector2 b, float amount)
        => Vector2.Lerp(a, b, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public Vector3 Interpolate(Vector3 a, Vector3 b, float amount)
        => Vector3.Lerp(a, b, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public Vector4 Interpolate(Vector4 a, Vector4 b, float amount)
        => Vector4.Lerp(a, b, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public Matrix3x2 Interpolate(Matrix3x2 a, Matrix3x2 b, float amount)
        => Matrix3x2.Lerp(a, b, CalculateExponentialLerpBlend(amount));

    /// <inheritdoc/>
    public Matrix4x4 Interpolate(Matrix4x4 a, Matrix4x4 b, float amount)
        => Matrix4x4.Lerp(a, b, CalculateExponentialLerpBlend(amount));
}

/// <summary>
/// An <see cref="IInterpolator"/> that performs linear interpolations between values
/// </summary>
public sealed class LinearInterpolator : IInterpolator
{
    private LinearInterpolator() { }

    /// <summary>
    /// Represents the singleton instance of this <see cref="IInterpolator"/>
    /// </summary>
    public static IInterpolator Interpolator => Instance;

    /// <summary>
    /// Same as <see cref="Interpolator"/>
    /// </summary>
    public static LinearInterpolator Instance { get; } = new LinearInterpolator();

    /// <inheritdoc/>
    public TValue Interpolate<TValue>(TValue a, TValue b, TValue amount) where TValue : INumber<TValue>, ILogarithmicFunctions<TValue>, IExponentialFunctions<TValue>, IHyperbolicFunctions<TValue>, IPowerFunctions<TValue>, IRootFunctions<TValue>, ITrigonometricFunctions<TValue>
        => Interpolation.InterpolateLinear<TValue>(a, b, amount);

    /// <inheritdoc/>
    public void VectorInterpolate<TValue>(Span<TValue> a, Span<TValue> b, Span<TValue> output, TValue amount) where TValue : INumber<TValue>, ILogarithmicFunctions<TValue>, IExponentialFunctions<TValue>, IHyperbolicFunctions<TValue>, IPowerFunctions<TValue>, IRootFunctions<TValue>, ITrigonometricFunctions<TValue>
        => Interpolation.InterpolateLinear<TValue>(a, b, output, amount);

    /// <inheritdoc/>
    public Vector2 Interpolate(Vector2 a, Vector2 b, float amount)
        => Vector2.Lerp(a, b, amount);

    /// <inheritdoc/>
    public Vector3 Interpolate(Vector3 a, Vector3 b, float amount)
        => Vector3.Lerp(a, b, amount);

    /// <inheritdoc/>
    public Vector4 Interpolate(Vector4 a, Vector4 b, float amount)
        => Vector4.Lerp(a, b, amount);

    /// <inheritdoc/>
    public Matrix3x2 Interpolate(Matrix3x2 a, Matrix3x2 b, float amount)
        => Matrix3x2.Lerp(a, b, amount);

    /// <inheritdoc/>
    public Matrix4x4 Interpolate(Matrix4x4 a, Matrix4x4 b, float amount)
        => Matrix4x4.Lerp(a, b, amount);
}

#endregion
