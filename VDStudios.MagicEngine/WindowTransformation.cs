using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents data necessary to transform rendered objects according to the current screen configuration
/// </summary>
public readonly struct WindowTransformation
{
    /// <summary>
    /// Represents the relative scaling of the window, to keep screen-relative coordinates properly scaled
    /// </summary>
    public readonly Matrix4x4 WindowScale { get; init; }
}
