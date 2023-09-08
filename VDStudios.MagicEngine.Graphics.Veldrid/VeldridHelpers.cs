using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Assorted helpers for Veldrid classes
/// </summary>
public static class VeldridHelpers
{
    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaFloat"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe RgbaFloat ToRgbaFloat(this RgbaVector vector) 
        => *(RgbaFloat*)(&vector);

    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaFloat"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref RgbaFloat ToRgbaFloatRef(this ref RgbaVector vector)
        => ref Unsafe.AsRef<RgbaFloat>(Unsafe.AsPointer(ref vector));

    /// <summary>
    /// Converts a <see cref="RgbaFloat"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe RgbaVector ToRgbaVector(this RgbaFloat vector)
        => *(RgbaVector*)(&vector);

    /// <summary>
    /// Converts a <see cref="RgbaFloat"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref RgbaVector ToRgbaVectorRef(this ref RgbaFloat vector)
        => ref Unsafe.AsRef<RgbaVector>(Unsafe.AsPointer(ref vector));
}
