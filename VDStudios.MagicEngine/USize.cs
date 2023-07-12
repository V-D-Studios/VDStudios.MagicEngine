namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an integer size with no negative components
/// </summary>
/// <param name="Width">The width represented by this instance</param>
/// <param name="Height">The height represented by this instance</param>
public readonly record struct USize(uint Width, uint Height);
