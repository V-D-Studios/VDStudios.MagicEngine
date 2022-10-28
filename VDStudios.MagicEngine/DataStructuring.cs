using System.Numerics;
using System.Runtime.CompilerServices;

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
    /// <typeparam name="TNumber">The type of the numeric representing the size</typeparam>
    /// <param name="elementCount">The amount of elements that will fit in the buffer</param>
    /// <returns>The appropriate buffer size necessary to fit the structures</returns>
    public static TNumber GetSize<TStruct, TNumber>(TNumber elementCount) where TStruct : unmanaged where TNumber : IBinaryInteger<TNumber>
        => (TNumber.CreateSaturating(Unsafe.SizeOf<TStruct>()) * elementCount);

    /// <summary>
    /// Gets the size of a blittable type <typeparamref name="TStruct"/> and fits it to the smallest possible size in bytes allowed by an uniform buffer
    /// </summary>
    /// <remarks>
    /// Uniform buffer sizes must be multiples of 16
    /// </remarks>
    /// <typeparam name="TStruct">The type to calculate the size for</typeparam>
    /// <typeparam name="TNumber">The type of the numeric representing the size</typeparam>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber FitToUniformBuffer<TStruct, TNumber>() where TStruct : unmanaged where TNumber : IBinaryInteger<TNumber>
        => TNumber.CreateSaturating(16u) * TNumber.CreateSaturating(Unsafe.SizeOf<TStruct>() / 16u + 1u);

    /// <summary>
    /// Fits <paramref name="size"/> to the smallest possible size in bytes allowed by an uniform buffer
    /// </summary>
    /// <remarks>
    /// Uniform buffer sizes must be multiples of 16
    /// </remarks>
    /// <param name="size"></param>
    /// <typeparam name="TNumber">The type of the numeric representing the size</typeparam>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber FitToUniformBuffer<TNumber>(TNumber size) where TNumber : IBinaryInteger<TNumber>
        => TNumber.CreateSaturating(16u) * (size / TNumber.CreateSaturating(16u) + TNumber.One);

    /// <summary>
    /// Gets the size of a blittable type <typeparamref name="TStruct"/> and fits it to the smallest possible size in bytes that is a multiple of <paramref name="multipleOf"/>
    /// </summary>
    /// <typeparam name="TStruct">The type to calculate the size for</typeparam>
    /// <typeparam name="TNumber">The type of the numeric representing the size</typeparam>
    /// <param name="multipleOf">The value to fit the size of <typeparamref name="TStruct"/> into</param>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber FitToSize<TStruct, TNumber>(TNumber multipleOf) where TStruct : unmanaged where TNumber : IBinaryInteger<TNumber>
        => multipleOf * (TNumber.CreateSaturating(Unsafe.SizeOf<TStruct>()) / multipleOf + TNumber.One);

    /// <summary>
    /// Fits <paramref name="size"/> to the smallest possible size in bytes that is a multiple of <paramref name="multipleOf"/>
    /// </summary>
    /// <typeparam name="TNumber">The type of the numeric representing the size</typeparam>
    /// <param name="multipleOf">The value to fit <paramref name="size"/> into</param>
    /// <param name="size">The actual size being tested</param>
    /// <returns>The appropriate buffer size necessary to fit the struct</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNumber FitToSize<TNumber>(TNumber size, TNumber multipleOf) where TNumber : IBinaryInteger<TNumber>
        => multipleOf * (TNumber.CreateSaturating(size) / multipleOf + TNumber.One);
}
