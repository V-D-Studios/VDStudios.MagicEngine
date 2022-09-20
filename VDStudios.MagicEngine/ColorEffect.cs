namespace VDStudios.MagicEngine;

/// <summary>
/// A bitmask representing the color effects for a <see cref="ColorTransformation"/>
/// </summary>
public enum ColorEffect : uint
{
    /// <summary>
    /// Translates all fragment colors into grayscale space
    /// </summary>
    GrayScale = 1 << 0,

    /// <summary>
    /// Enables <see cref="ColorTransformation.Tint"/> to be applied over fragments
    /// </summary>
    /// <remarks>
    /// Works best if used with <see cref="GrayScale"/>
    /// </remarks>
    Tinted = 1 << 1,

    /// <summary>
    /// Enables <see cref="ColorTransformation.Overlay"/> to be applied over fragments
    /// </summary>
    Overlay = 1 << 2,
}
