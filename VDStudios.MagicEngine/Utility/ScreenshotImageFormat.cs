using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents the format of a screenshot image of a <see cref="Graphics.GraphicsManager{TGraphicsContext}"/>
/// </summary>
public enum ScreenshotImageFormat : byte
{
    /// <summary>
    /// BMP or Bitmap, a file format for bitmap digital images
    /// </summary>
    BMP,

    /// <summary>
    /// JPG or JPEG, a commonly used format of lossy compression for digital images
    /// </summary>
    JPG,

    /// <summary>
    /// Portable Network Graphics, a file format that supports lossless data compression for images
    /// </summary>
    PNG
}
