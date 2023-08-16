namespace VDStudios.MagicEngine.Input;

/// <summary>
/// Represents a mouse button
/// </summary>
/// <remarks>
/// Modeled after SDL
/// </remarks>
[Flags]
public enum MouseButton
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    Left = 1,
    Middle = 1 << 1,
    Right = 1 << 2,
    X1 = 1 << 3,
    X2 = 1 << 4
}
