namespace VDStudios.MagicEngine;

/// <summary>
/// A bitmask representing the color effects for a <see cref="ColorTransformation"/>
/// </summary>
public enum ColorEffect : byte
{
    /// <summary>
    /// Enables <see cref="ColorTransformation.Tint"/> to be applied over fragments
    /// </summary>
    Tinted = 1 << 0,

    /// <summary>
    /// Enables <see cref="ColorTransformation.Overlay"/> to be applied over fragments
    /// </summary>
    Overlay = 1 << 1,
}
