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
        /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/> using this object's <see cref="VectorInterpolate{TValue}(Span{TValue}, Span{TValue}, Span{TValue}, TValue)"/> method
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
        /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/> using this object's <see cref="VectorInterpolate{TValue}(Span{TValue}, Span{TValue}, Span{TValue}, TValue)"/> method
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
        /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/> using this object's <see cref="VectorInterpolate{TValue}(Span{TValue}, Span{TValue}, Span{TValue}, TValue)"/> method
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
        /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/> using this object's <see cref="VectorInterpolate{TValue}(Span{TValue}, Span{TValue}, Span{TValue}, TValue)"/> method
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
        /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="amount"/> using this object's <see cref="VectorInterpolate{TValue}(Span{TValue}, Span{TValue}, Span{TValue}, TValue)"/> method
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
    /// An <see cref="IInterpolator"/> that performs linear interpolations between values
    /// </summary>
    public sealed class Linear : IInterpolator
    {
        private Linear() { }

        /// <summary>
        /// Represents the singleton instance of this <see cref="IInterpolator"/>
        /// </summary>
        public static IInterpolator Interpolator => LinearInterpolator;

        /// <summary>
        /// Same as <see cref="Interpolator"/>
        /// </summary>
        public static Linear LinearInterpolator { get; } = new Linear();

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
        public unsafe Vector3 Interpolate(Vector3 a, Vector3 b, float amount)
            => Vector3.Lerp(a, b, amount);

        /// <inheritdoc/>
        public unsafe Vector4 Interpolate(Vector4 a, Vector4 b, float amount)
            => Vector4.Lerp(a, b, amount);

        /// <inheritdoc/>
        public unsafe Matrix3x2 Interpolate(Matrix3x2 a, Matrix3x2 b, float amount)
            => Matrix3x2.Lerp(a, b, amount);

        /// <inheritdoc/>
        public unsafe Matrix4x4 Interpolate(Matrix4x4 a, Matrix4x4 b, float amount)
            => Matrix4x4.Lerp(a, b, amount);
    }

    #endregion

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
