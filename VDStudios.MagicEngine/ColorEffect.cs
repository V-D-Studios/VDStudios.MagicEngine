namespace VDStudios.MagicEngine;

/// <summary>
/// A bitmask representing the color effects for a <see cref="ColorTransformation"/>
/// </summary>
public enum ColorEffect : uint
{
    /// <summary>
    /// Translates all fragment colors into grayscale space
    /// </summary>
    GrayScale = 1 << 0, // 0000_000___0000_0000___0000_000___0000_0001

    /// <summary>
    /// Enables <see cref="ColorTransformation.Tint"/> to be applied over fragments
    /// </summary>
    /// <remarks>
    /// Works best if used with <see cref="GrayScale"/>
    /// </remarks>
    Tinted = 1 << 1, // 0000_000___0000_0000___0000_000___0000_0010

    /// <summary>
    /// Enables <see cref="ColorTransformation.Overlay"/> to be applied over fragments
    /// </summary>
    Overlay = 1 << 2, // 0000_000___0000_0000___0000_000___0000_0100

    /// <summary>
    /// Enables <see cref="ColorTransformation.Opacity"/> to override the final fragment's alpha value
    /// </summary>
    /// <remarks>
    /// If both <see cref="OpacityMultiply"/> and <see cref="OpacityOverride"/> are set, <see cref="OpacityOverride"/> takes precedence
    /// </remarks>
    OpacityOverride = 1 << 3, // 0000_000___0000_0000___0000_000___0000_1000

    /// <summary>
    /// Enables <see cref="ColorTransformation.Opacity"/> to set the fragment's final alpha value to the product of itself and its current alpha value
    /// </summary>
    /// <remarks>
    /// If both <see cref="OpacityMultiply"/> and <see cref="OpacityOverride"/> are set, <see cref="OpacityOverride"/> takes precedence
    /// </remarks>
    OpacityMultiply = 1 << 4, // 0000_000___0000_0000___0000_000___0001_0000
}
