using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using SixLabors.ImageSharp.PixelFormats;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using static System.Net.Mime.MediaTypeNames;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A set of utilities around SDL types for Veldrid
/// </summary>
public static class SDLUtilities
{
    // // // The produced textures are wrong, completely wrong. Like, cursed wrong
    ///// <summary>
    ///// Converts the <paramref name="surface"/> into a Veldrid Texture through <paramref name="context"/>
    ///// </summary>
    ///// <param name="surface">The surface to convert</param>
    ///// <param name="context">The graphics context representing the device to create the texture for</param>
    ///// <param name="type">The texture type</param>
    ///// <param name="usage">The texture usage</param>
    ///// <param name="sampleCount">The sample count of the texture</param>
    ///// <exception cref="InvalidOperationException"></exception>
    //public static unsafe Texture ToTexture(this Surface surface, IVeldridGraphicsContextResources context, TextureType type, 
    //    TextureUsage usage = TextureUsage.Sampled, TextureSampleCount sampleCount = TextureSampleCount.Count1)
    //{
    //    PixelFormatData sformat = surface.GetFormat();

    //    var pformat = sformat.BytesPerPixel == 4
    //        ? sformat.Mask.Red == 0x000000ff ? PixelFormat.R8_G8_B8_A8_UNorm : PixelFormat.B8_G8_R8_A8_UNorm
    //        : throw new InvalidOperationException("Pixel formats with no alpha are not supported");

    //    //if (sformat.Mask.Red == 0x000000ff)
    //    //    pformat = PixelFormat.RGB;
    //    //else
    //    //    pformat = PixelFormat.BGR;
    //    // // In case of no alpha, but couldn't find a format equivalent to RGB/BGR, only compressed or RGBA/BGRA

    //    var size = surface.Size;
    //    var bpp = sformat.BytesPerPixel;
    //    var tex = context.ResourceFactory.CreateTexture(new TextureDescription(
    //       (uint)size.Width,
    //       (uint)size.Height,
    //       1,
    //       1,
    //       1,
    //       pformat,
    //       usage,
    //       type,
    //       sampleCount
    //    ));

    //    var gd = context.GraphicsDevice;
    //    var pixelSpan = surface.GetPixels(out _);
    //    fixed (void* pin = pixelSpan)
    //    {
    //        gd.UpdateTexture(
    //            tex,
    //            (IntPtr)pin,
    //            (uint)(bpp * size.Width * size.Height),
    //            0,
    //            0,
    //            0,
    //            (uint)size.Width,
    //            (uint)size.Height,
    //            1,
    //            0,
    //            0);
    //    }

    //    return tex;
    //}
}
