using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents extra math utilities that can be used on <see cref="float"/> values and <see cref="Vector2"/> values
/// </summary>
public static class MathUtils
{
    /// <summary>
    /// Gets the size of a blittable type <typeparamref name="TStruct"/> and fits it to the smallest possible size in bytes allowed by an uniform buffer
    /// </summary>
    /// <remarks>
    /// Uniform buffer sizes must be multiples of 16
    /// </remarks>
    /// <typeparam name="TStruct">The type to calculate the size for</typeparam>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FitToUniformBuffer<TStruct>() where TStruct : unmanaged
        => 16u * ((uint)Unsafe.SizeOf<TStruct>() / 16u + 1u);

    /// <summary>
    /// Gets the size of a blittable type <typeparamref name="TStruct"/> and fits it to the smallest possible size in bytes that is a multiple of <paramref name="multipleOf"/>
    /// </summary>
    /// <typeparam name="TStruct">The type to calculate the size for</typeparam>
    /// <param name="multipleOf">The value to fit the size of <typeparamref name="TStruct"/> into</param>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FitToSize<TStruct>(uint multipleOf) where TStruct : unmanaged
        => multipleOf * ((uint)Unsafe.SizeOf<TStruct>() / multipleOf + 1u);

    /// <summary>
    /// Calculates the angle in radians between a line that passes through <paramref name="a"/> and <paramref name="b"/> and a line that passes through <paramref name="a"/> and <paramref name="c"/>
    /// </summary>
    /// <param name="a">The point at which the line <paramref name="b"/><paramref name="a"/> and <paramref name="c"/><paramref name="a"/> intersect</param>
    /// <param name="b">The second point <paramref name="b"/><paramref name="a"/> passes through</param>
    /// <param name="c">The second point <paramref name="c"/><paramref name="a"/> passes through</param>
    /// <returns>The angle in radians between <paramref name="b"/><paramref name="a"/> and <paramref name="c"/><paramref name="a"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(Vector2 b, Vector2 a, Vector2 c)
    {
        var ba = b - a;
        var ca = c - a;
        return MathF.Abs(MathF.Atan2(Cross(ba, ca), Vector2.Dot(ba, ca)));
    }

    /// <summary>
    /// Calculates the cross product between <see cref="Vector2"/>s <paramref name="a"/> and <paramref name="b"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
