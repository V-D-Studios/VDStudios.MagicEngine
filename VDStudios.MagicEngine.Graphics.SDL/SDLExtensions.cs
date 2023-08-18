using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A set of assorted extensions for better SDL and MagicEngine interoperability
/// </summary>
public static class SDLExtensions
{
    /// <summary>
    /// Converts a <see cref="RgbaVector"/> into equivalent values for a <see cref="RGBAColor"/>
    /// </summary>
    public static RGBAColor ToRGBAColor(this RgbaVector vector)
    {
        return new RGBAColor(Convert(vector.R), Convert(vector.G), Convert(vector.B), Convert(vector.A));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte Convert(float value)
            => (byte)float.Clamp(value * 255, 0, 255);
    }

    /// <summary>
    /// Converts a <see cref="RGBAColor"/> into equivalent values for a <see cref="RgbaVector"/>
    /// </summary>
    public static RgbaVector ToRgbaVector(this RGBAColor vector)
        => new(vector.Red / 255f, vector.Green / 255f, vector.Blue / 255f, vector.Alpha / 255f);
}
