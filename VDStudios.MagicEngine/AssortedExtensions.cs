using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Timing;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a set of loosely related extensions
/// </summary>
public static class AssortedExtensions
{
    /// <summary>
    /// Restarts the timer, setting <see cref="GameFrameTimer.Clocks"/> back to 0 and starting the count anew by producing a new one
    /// </summary>
    public static void Restart(ref this GameFrameTimer timer)
        => timer = timer.RestartNew();

    /// <summary>
    /// Restarts the timer, setting <see cref="GameFrameTimer.Clocks"/> back to 0 and starting the count anew by producing a new one
    /// </summary>
    public static void Restart(ref this GraphicsManagerFrameTimer timer)
        => timer = timer.RestartNew();

    /// <summary>
    /// Represents the largest prime number that is less than <see cref="ulong.MaxValue"/>
    /// </summary>
    public const ulong PrimeNumberNearestToUInt64MaxValue = 18446744073709551557;

    /// <summary>
    /// Represents the largest prime number that is less than <see cref="uint.MaxValue"/>
    /// </summary>
    public const uint PrimeNumberNearestToUInt32MaxValue = 4294967291;

    /// <summary>
    /// Represents the largest prime number that is less than <see cref="ushort.MaxValue"/>
    /// </summary>
    public const ushort PrimeNumberNearestToUInt16MaxValue = 65521;

    /// <summary>
    /// Deconstructs <paramref name="vec"/> in the following order: <c>X</c>, <c>Y</c>, <c>Z</c>, <c>W</c>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Vector4 vec, out float x, out float y, out float z, out float w)
    {
        x = vec.X;
        y = vec.Y;
        z = vec.Z;
        w = vec.W;
    }

    /// <summary>
    /// Deconstructs <paramref name="vec"/> in the following order: <c>X</c>, <c>Y</c>, <c>Z</c>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Vector3 vec, out float x, out float y, out float z)
    {
        x = vec.X;
        y = vec.Y;
        z = vec.Z;
    }

    /// <summary>
    /// Deconstructs <paramref name="vec"/> in the following order: <c>X</c>, <c>Y</c>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Vector2 vec, out float x, out float y)
    {
        x = vec.X;
        y = vec.Y;
    }

    /// <summary>
    /// Creates a new <see cref="Vector2"/> with the <see cref="Vector3.X"/> and <see cref="Vector3.Y"/> values of <paramref name="vec"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Vector3 vec)
        => new(vec.X, vec.Y);

    /// <summary>
    /// This method serves a shorthand for calling <see cref="object.GetType"/> on <paramref name="obj"/> and taking <see cref="MemberInfo.Name"/>
    /// </summary>
    /// <param name="obj">The object to get the type name from</param>
    /// <returns>The type name of <paramref name="obj"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetTypeName(this object obj) => obj.GetType().Name;

    /// <summary>
    /// Gets <see cref="Vector2.X"/> from <paramref name="vector"/>
    /// </summary>
    /// <remarks>
    /// Functions as an utility to ensure any instance of a <see cref="Vector2"/> being used as a size parameter fetches the correct field
    /// </remarks>
    public static float Width(this Vector2 vector)
        => vector.X;

    /// <summary>
    /// Gets <see cref="Vector2.Y"/> from <paramref name="vector"/>
    /// </summary>
    /// <remarks>
    /// Functions as an utility to ensure any instance of a <see cref="Vector2"/> being used as a size parameter fetches the correct field
    /// </remarks>
    public static float Height(this Vector2 vector)
        => vector.Y;

    /// <summary>
    /// Gets the bytes associated to a given <see cref="string"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetUTF8Bytes(this string str)
        => Encoding.UTF8.GetBytes(str);

    /// <summary>
    /// Takes the <see cref="Vector2"/> pointer and converts it into a span of 2 <see cref="float"/>s
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(Vector2* vector)
        => new(vector, 2);

    /// <summary>
    /// Takes the <see cref="Vector2"/> pointer and converts it into a span of 2 <see cref="float"/>s
    /// </summary>
    /// <remarks>
    /// This method must not be used when <paramref name="vector"/> is a <c>ref</c> to a <c>field</c>, as it's not pinned by the GC and may be moved; leading to potential problems such as no changes being presented in the Vector, or corrupting memory elsewhere in the program. For such cases, pin the vector using the <c>fixed</c> keyword taking its reference with the <c>&amp;</c> operator, and pass it to <see cref="AsSpan(Vector2*)"/>. <c>ref</c>s to local variables are acceptable, and intended.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(this scoped ref Vector2 vector)
    {
        fixed (void* ptr = &vector)
            return new((float*)ptr, 2);
    }

    /// <summary>
    /// Takes the <see cref="Vector3"/> pointer and converts it into a span of 3 <see cref="float"/>s
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(Vector3* vector)
        => new(vector, 2);

    /// <summary>
    /// Takes the <see cref="Vector3"/> pointer and converts it into a span of 3 <see cref="float"/>s
    /// </summary>
    /// <remarks>
    /// This method must not be used when <paramref name="vector"/> is a <c>ref</c> to a <c>field</c>, as it's not pinned by the GC and may be moved; leading to potential problems such as no changes being presented in the Vector, or corrupting memory elsewhere in the program. For such cases, pin the vector using the <c>fixed</c> keyword taking its reference with the <c>&amp;</c> operator, and pass it to <see cref="AsSpan(Vector3*)"/>. <c>ref</c>s to local variables are acceptable, and intended.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(this scoped ref Vector3 vector)
    {
        fixed (void* ptr = &vector)
            return new((float*)ptr, 2);
    }

    /// <summary>
    /// Takes the <see cref="Vector4"/> pointer and converts it into a span of 4 <see cref="float"/>s
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(Vector4* vector)
        => new(vector, 2);

    /// <summary>
    /// Takes the <see cref="Vector4"/> pointer and converts it into a span of 4 <see cref="float"/>s
    /// </summary>
    /// <remarks>
    /// This method must not be used when <paramref name="vector"/> is a <c>ref</c> to a <c>field</c>, as it's not pinned by the GC and may be moved; leading to potential problems such as no changes being presented in the Vector, or corrupting memory elsewhere in the program. For such cases, pin the vector using the <c>fixed</c> keyword taking its reference with the <c>&amp;</c> operator, and pass it to <see cref="AsSpan(Vector4*)"/>. <c>ref</c>s to local variables are acceptable, and intended.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<float> AsSpan(this scoped ref Vector4 vector)
    {
        fixed (void* ptr = &vector)
            return new((float*)ptr, 2);
    }
}
