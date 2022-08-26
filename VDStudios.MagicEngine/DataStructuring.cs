using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Contains an assortment of helper and extension methods in relation to data size, unmanaged structure datatypes, and buffers
/// </summary>
public static class DataStructuring
{
    /// <summary>
    /// Calculates the buffer size necessary to hold <paramref name="elementCount"/> elements of type <typeparamref name="TStruct"/>
    /// </summary>
    /// <typeparam name="TStruct">The type to calculate the size for</typeparam>
    /// <param name="elementCount">The amount of elements that will fit in the buffer</param>
    /// <returns>The appropriate buffer size necessary to fit the structures</returns>
    public static uint GetSize<TStruct>(uint elementCount) where TStruct : unmanaged
        => (uint)Unsafe.SizeOf<TStruct>() * elementCount;

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
}
