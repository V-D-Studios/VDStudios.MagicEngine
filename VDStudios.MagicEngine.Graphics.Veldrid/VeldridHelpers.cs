using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Assorted helpers for Veldrid classes
/// </summary>
public static class VeldridHelpers
{
    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe RgbaVector ToRgbaFloat(this RgbaVector vector) 
        => *(RgbaVector*)(&vector);

    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref RgbaVector ToRgbaFloatRef(this ref RgbaVector vector)
        => ref Unsafe.AsRef<RgbaVector>(Unsafe.AsPointer(ref vector));

    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe RgbaVector ToRgbaVector(this RgbaVector vector)
        => *(RgbaVector*)(&vector);

    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into a Veldrid <see cref="RgbaVector"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref RgbaVector ToRgbaVectorRef(this ref RgbaVector vector)
        => ref Unsafe.AsRef<RgbaVector>(Unsafe.AsPointer(ref vector));
}
